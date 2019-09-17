// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: the CompositionManager class
//
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Runtime.InteropServices;

using MS.Win32;
using Microsoft.Win32; // for RegistryKey class
using MS.Internal ; 
using MS.Internal.PresentationCore;                        // SecurityHelper

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    //
    // Modes of AltNumpad.
    //
    internal enum AltNumpadConversionMode
    {
        DefaultCodePage,         // ACP code page encoding. Alt+Numpad0+NumpadX
        OEMCodePage,             // OEM code page encoding. Alt+NumpadX
        HexDefaultCodePage,      // HEX value in ACP.       Alt+NumpadDOT+NumpadX
        HexUnicode,              // HEX value in Unicode.   Alt+NumpadPlus+NumpadX
    }

    /// <summary>
    ///     the CompositionManager class provides input-text/composition event promotion
    /// </summary>
    public sealed class TextCompositionManager : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  static RoutedEvent
        //
        //------------------------------------------------------

        /// <summary>
        ///     Preview Composition Start
        /// </summary>
        public static readonly RoutedEvent PreviewTextInputStartEvent = EventManager.RegisterRoutedEvent("PreviewTextInputStart", RoutingStrategy.Tunnel, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the PreviewTextInputStart attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewTextInputStartHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, PreviewTextInputStartEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewTextInputStart attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewTextInputStartHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, PreviewTextInputStartEvent, handler);
        }

        /// <summary>
        ///     Composition Start
        /// </summary>
        public static readonly RoutedEvent TextInputStartEvent = EventManager.RegisterRoutedEvent("TextInputStart", RoutingStrategy.Bubble, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the TextInputStart attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddTextInputStartHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, TextInputStartEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the TextInputStart attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveTextInputStartHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, TextInputStartEvent, handler);
        }

        /// <summary>
        ///     Preview Composition Updated
        /// </summary>
        public static readonly RoutedEvent PreviewTextInputUpdateEvent = EventManager.RegisterRoutedEvent("PreviewTextInputUpdate", RoutingStrategy.Tunnel, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the PreviewTextInputUpdate attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewTextInputUpdateHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, PreviewTextInputUpdateEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewTextInputUpdate attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewTextInputUpdateHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, PreviewTextInputUpdateEvent, handler);
        }

        /// <summary>
        ///     Composition Updated
        /// </summary>
        public static readonly RoutedEvent TextInputUpdateEvent = EventManager.RegisterRoutedEvent("TextInputUpdate", RoutingStrategy.Bubble, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the TextInputUpdate attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddTextInputUpdateHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, TextInputUpdateEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the TextInputUpdate attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveTextInputUpdateHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, TextInputUpdateEvent, handler);
        }

        /// <summary>
        ///     Preview Composition End
        /// </summary>
        public static readonly RoutedEvent PreviewTextInputEvent = EventManager.RegisterRoutedEvent("PreviewTextInput", RoutingStrategy.Tunnel, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the PreviewTextInput attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewTextInputHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, PreviewTextInputEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewTextInput attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewTextInputHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, PreviewTextInputEvent, handler);
        }

        /// <summary>
        ///     Composition End
        /// </summary>
        public static readonly RoutedEvent TextInputEvent = EventManager.RegisterRoutedEvent("TextInput", RoutingStrategy.Bubble, typeof(TextCompositionEventHandler), typeof(TextCompositionManager));

        /// <summary>
        ///     Adds a handler for the TextInput attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddTextInputHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.AddHandler(element, TextInputEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the TextInput attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveTextInputHandler(DependencyObject element, TextCompositionEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            UIElement.RemoveHandler(element, TextInputEvent, handler);
        }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextCompositionManager(InputManager inputManager)
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

        /// <summary>
        ///     Start the composition.
        /// </summary>
        public static bool StartComposition(TextComposition composition)
        {
            return UnsafeStartComposition(composition);
        }

        /// <summary>
        ///     Update the composition.
        /// </summary>
        public static bool UpdateComposition(TextComposition composition)
        {
            return UnsafeUpdateComposition(composition);
        }

        /// <summary>
        ///     Complete the composition.
        /// </summary>
        public static bool CompleteComposition(TextComposition composition)
        {
            return UnsafeCompleteComposition(composition);
        }

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

        private static bool UnsafeStartComposition(TextComposition composition)
        {
            if (composition == null)
            {
                throw new ArgumentNullException("composition");
            }

            if (composition._InputManager == null)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_NoInputManager, "composition"));
            }

            if (composition.Stage != TextCompositionStage.None)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_TextCompositionHasStarted, "composition"));
            }

            composition.Stage = TextCompositionStage.Started;
            TextCompositionEventArgs textargs = new TextCompositionEventArgs(composition._InputDevice, composition);
            textargs.RoutedEvent=TextCompositionManager.PreviewTextInputStartEvent;
            textargs.Source= composition.Source;
            return composition._InputManager.ProcessInput(textargs);
        }

        private static bool UnsafeUpdateComposition(TextComposition composition)
        {
            if (composition == null)
            {
                throw new ArgumentNullException("composition");
            }

            if (composition._InputManager == null)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_NoInputManager, "composition"));
            }

            if (composition.Stage == TextCompositionStage.None)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_TextCompositionNotStarted, "composition"));
            }

            if (composition.Stage == TextCompositionStage.Done)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_TextCompositionHasDone, "composition"));
            }

            TextCompositionEventArgs textargs = new TextCompositionEventArgs(composition._InputDevice, composition);
            textargs.RoutedEvent=TextCompositionManager.PreviewTextInputUpdateEvent;
            textargs.Source= composition.Source;
            return composition._InputManager.ProcessInput(textargs);
        }

        private static bool UnsafeCompleteComposition(TextComposition composition)
        {
            if (composition == null)
            {
                throw new ArgumentNullException("composition");
            }

            if (composition._InputManager == null)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_NoInputManager, "composition"));
            }

            if (composition.Stage == TextCompositionStage.None)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_TextCompositionNotStarted, "composition"));
            }

            if (composition.Stage == TextCompositionStage.Done)
            {
                throw new ArgumentException(SR.Get(SRID.TextCompositionManager_TextCompositionHasDone, "composition"));
            }

            composition.Stage = TextCompositionStage.Done;
            TextCompositionEventArgs textargs = new TextCompositionEventArgs(composition._InputDevice, composition);
            textargs.RoutedEvent=TextCompositionManager.PreviewTextInputEvent;
            textargs.Source= composition.Source;
            return composition._InputManager.ProcessInput(textargs);
        }

        private static string GetCurrentOEMCPEncoding(int code)
        {
            int cp =  UnsafeNativeMethods.GetOEMCP();
            return CharacterEncoding(cp, code);
        }

        // Convert code to the string based on the code page.
        private static string CharacterEncoding(int cp, int code)
        {
            Byte[] bytes = ConvertCodeToByteArray(code);
            StringBuilder sbuilder = new StringBuilder(EncodingBufferLen);

            // Win32K uses MB_PRECOMPOSED | MB_USEGLYPHCHARS.
            int nret = UnsafeNativeMethods.MultiByteToWideChar(cp, 
                                                   UnsafeNativeMethods.MB_PRECOMPOSED | UnsafeNativeMethods.MB_USEGLYPHCHARS,
                                                   bytes, bytes.Length,
                                                   sbuilder, EncodingBufferLen);

            if (nret == 0)
            {
                int win32Err = Marshal.GetLastWin32Error(); 
                throw new System.ComponentModel.Win32Exception(win32Err);
            }

            // set the length as MultiByteToWideChar returns.
            sbuilder.Length = nret;
            return sbuilder.ToString();
        }

        // PreProcessInput event handler
        private void PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            // KeyDown --> Alt Numpad
            //
            // We eat Alt-NumPat keys and handle them by ourselves. Avalon has its own acceralator handler
            // and it may have a corrision with Win32k's AltNumPad handling. As a result, the AltNumPad cache
            // in Win32k's ToUnicodeEx() could be broken.
            //
            if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent)
            {
                KeyEventArgs keyArgs = (KeyEventArgs) e.StagingItem.Input;
                if (!keyArgs.Handled)
                {
                    if (!_altNumpadEntryMode)
                    {
                        EnterAltNumpadEntryMode(keyArgs.RealKey);
                    }
                    else
                    {
                        if (HandleAltNumpadEntry(keyArgs.RealKey, keyArgs.ScanCode, keyArgs.IsExtendedKey))
                        {
                            if (_altNumpadcomposition == null)
                            {
                                _altNumpadcomposition = new TextComposition(_inputManager, (IInputElement)keyArgs.Source, "", TextCompositionAutoComplete.Off, keyArgs.Device);
                                keyArgs.Handled = UnsafeStartComposition(_altNumpadcomposition);
                            }
                            else
                            {
                                _altNumpadcomposition.ClearTexts();
                                keyArgs.Handled = UnsafeUpdateComposition(_altNumpadcomposition);
                            }

                            // We ate this key for AltNumPad entry. None will be able to handle this.
                            e.Cancel();
                        }
                        else 
                        {
                            // alt numpad entry was reset so composition needs to be finalized.
                            if (_altNumpadcomposition != null)
                            {
                                _altNumpadcomposition.ClearTexts();
                                _altNumpadcomposition.Complete();
                                ClearAltnumpadComposition();
                            }
                        }
                    }
}
}

            if (e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyDownEvent)
            {
                KeyEventArgs keyArgs = (KeyEventArgs)e.StagingItem.Input;
                //This makes sure that deadChar's do not get handled in commands
                //As a result they are unhandled KeyDown events that are sent to translate input.
                //
                if (!keyArgs.Handled
                    && (_deadCharTextComposition != null)
                    && (_deadCharTextComposition.Stage == TextCompositionStage.Started))
                {
                    keyArgs.MarkDeadCharProcessed();
                }
            }
        }
        
        // PostProcessInput event handler
        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            // KeyUp
            if(e.StagingItem.Input.RoutedEvent == Keyboard.KeyUpEvent)
            {
                KeyEventArgs keyArgs = (KeyEventArgs) e.StagingItem.Input;
                if(!keyArgs.Handled)
                {
                    if(keyArgs.RealKey == Key.LeftAlt || keyArgs.RealKey == Key.RightAlt)
                    {
                        // Make sure both Alt keys are up.
                        ModifierKeys modifiers = keyArgs.KeyboardDevice.Modifiers;
                        if((modifiers & ModifierKeys.Alt) == 0)
                        {
                            if(_altNumpadEntryMode)
                            {
                                _altNumpadEntryMode = false;

                                // Generate the Unicode equivalent if we
                                // actually entered a number via the numpad.
                                if(_altNumpadEntry != 0)
                                {
                                    _altNumpadcomposition.ClearTexts();
                                    if (_altNumpadConversionMode == AltNumpadConversionMode.OEMCodePage)
                                    {
                                        _altNumpadcomposition.SetText(GetCurrentOEMCPEncoding(_altNumpadEntry));
                                    }
                                    else if ((_altNumpadConversionMode == AltNumpadConversionMode.DefaultCodePage) ||
                                             (_altNumpadConversionMode == AltNumpadConversionMode.HexDefaultCodePage))
                                    {
                                        _altNumpadcomposition.SetText(CharacterEncoding(InputLanguageManager.Current.CurrentInputLanguage.TextInfo.ANSICodePage, _altNumpadEntry));
                                    }
                                    else if (_altNumpadConversionMode == AltNumpadConversionMode.HexUnicode)
                                    {
                                        Char[] chars = new Char[1];
                                        chars[0] = (Char) _altNumpadEntry;
                                        _altNumpadcomposition.SetText(new string(chars));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Someone handled Alt key up event, we cancel Alt-Numpad handling.
                    _altNumpadEntryMode = false;
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;
                }
            }

            // PreviewTextInputBegin --> TextInputStart
            else if(e.StagingItem.Input.RoutedEvent == TextCompositionManager.PreviewTextInputStartEvent)
            {
                TextCompositionEventArgs textArgs = (TextCompositionEventArgs) e.StagingItem.Input;
                if(!textArgs.Handled)
                {
                    TextCompositionEventArgs text = new TextCompositionEventArgs(textArgs.Device, textArgs.TextComposition);
                    text.RoutedEvent=TextCompositionManager.TextInputStartEvent;
                    text.Source= textArgs.TextComposition.Source;
                    e.PushInput(text, e.StagingItem);
                }
            }

            // PreviewTextInputUpdate --> TextInputUpdate
            else if(e.StagingItem.Input.RoutedEvent == TextCompositionManager.PreviewTextInputUpdateEvent)
            {
                TextCompositionEventArgs textArgs = (TextCompositionEventArgs) e.StagingItem.Input;
                if(!textArgs.Handled)
                {
                    TextCompositionEventArgs text = new TextCompositionEventArgs(textArgs.Device, textArgs.TextComposition);
                    text.RoutedEvent=TextCompositionManager.TextInputUpdateEvent;
                    text.Source= textArgs.TextComposition.Source;
                    e.PushInput(text, e.StagingItem);
                }
            }

            // PreviewTextInput --> TextInput
            else if(e.StagingItem.Input.RoutedEvent == TextCompositionManager.PreviewTextInputEvent)
            {
                TextCompositionEventArgs textArgs = (TextCompositionEventArgs) e.StagingItem.Input;
                if(!textArgs.Handled)
                {
                    TextCompositionEventArgs text = new TextCompositionEventArgs(textArgs.Device, textArgs.TextComposition);
                    text.RoutedEvent=TextCompositionManager.TextInputEvent;
                    text.Source= textArgs.TextComposition.Source;
                    e.PushInput(text, e.StagingItem);
                }
            }

            // TextCompositioniBegin --> TextInput if this is AutomaticComplete.
            else if(e.StagingItem.Input.RoutedEvent == TextCompositionManager.TextInputStartEvent)
            {
                TextCompositionEventArgs textArgs = (TextCompositionEventArgs) e.StagingItem.Input;
                if(!textArgs.Handled)
                {
                    if (textArgs.TextComposition.AutoComplete == TextCompositionAutoComplete.On)
                    {
                        textArgs.Handled = UnsafeCompleteComposition(textArgs.TextComposition);
                    }
                }
            }

            // TextCompositionUpdate --> TextInput if this is AutomaticComplete.
            else if(e.StagingItem.Input.RoutedEvent == TextCompositionManager.TextInputUpdateEvent)
            {
                TextCompositionEventArgs textArgs = (TextCompositionEventArgs) e.StagingItem.Input;
                if(!textArgs.Handled)
                {
                    if ((textArgs.TextComposition == _deadCharTextComposition) &&
                         (_deadCharTextComposition.Composed))
                    {
                        //Clear the _deadCharTextComposition to handle re-entrant cases.
                        DeadCharTextComposition comp = _deadCharTextComposition;
                        _deadCharTextComposition = null;
                        textArgs.Handled = UnsafeCompleteComposition(comp);
                    }
                }
            }

            
            // Raw to StartComposition.
            InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;
            if(input != null)
            {
                if(input.Report.Type == InputType.Text && input.RoutedEvent == InputManager.InputReportEvent)
                {
                    RawTextInputReport textInput;
                    textInput = (RawTextInputReport)input.Report;

                    // 
                    // 



                    string inputText = new string(textInput.CharacterCode, 1);
                    bool fDoneAltNumpadComposition = false;

                    if (_altNumpadcomposition != null)
                    {
                        // Generate TextInput event from WM_CHAR handler.
                        if (inputText.Equals(_altNumpadcomposition.Text))
                        {
                            fDoneAltNumpadComposition = true;
                        }
                        else
                        {
                            // The generated text from InputReport does not matched with _altNumpadcomposition.
                            // Cancel this composition and process the char from InputReport.
                            _altNumpadcomposition.ClearTexts();
                        }

                        _altNumpadcomposition.Complete();
                        ClearAltnumpadComposition();
                    }

                    if (!fDoneAltNumpadComposition)
                    {
                        if (textInput.IsDeadCharacter)
                        {
                            _deadCharTextComposition = new DeadCharTextComposition(_inputManager, (IInputElement)null, inputText , TextCompositionAutoComplete.Off, InputManager.Current.PrimaryKeyboardDevice);
                            if (textInput.IsSystemCharacter)
                            {
                                _deadCharTextComposition.MakeSystem();
                            }
                            else if (textInput.IsControlCharacter)
                            {
                                _deadCharTextComposition.MakeControl();
                            }

                            input.Handled = UnsafeStartComposition(_deadCharTextComposition);
                        }
                        else
                        {
                            if (_deadCharTextComposition != null)
                            {
                                input.Handled = CompleteDeadCharComposition(inputText,
                                                                            textInput.IsSystemCharacter,
                                                                            textInput.IsControlCharacter);
                            }
                            else
                            {
                                TextComposition composition = new TextComposition(_inputManager, (IInputElement)e.StagingItem.Input.Source, inputText, TextCompositionAutoComplete.On, InputManager.Current.PrimaryKeyboardDevice);
                                if (textInput.IsSystemCharacter)
                                {
                                    composition.MakeSystem();
                                }
                                else if (textInput.IsControlCharacter)
                                {
                                    composition.MakeControl();
                                }
                                input.Handled = UnsafeStartComposition(composition);
                            }
                        }
                    }
                }
            }
        }


        internal void CompleteDeadCharComposition()
        {
            CompleteDeadCharComposition(String.Empty, false, false);
        }

        private bool CompleteDeadCharComposition(string inputText,
                                                              bool isSystemCharacter,
                                                              bool isControlCharacter)
        {
            if (_deadCharTextComposition != null)
            {
                _deadCharTextComposition.ClearTexts();
                _deadCharTextComposition.SetText(inputText);
                _deadCharTextComposition.Composed = true;
                if (isSystemCharacter)
                {
                    _deadCharTextComposition.MakeSystem();
                }
                else if (isControlCharacter)
                {
                    _deadCharTextComposition.MakeControl();
                }

                return UnsafeUpdateComposition(_deadCharTextComposition);
            }

            return false;
        }

        // AltNumpad key handler
        private bool EnterAltNumpadEntryMode(Key key)
        {
            bool handled = false;
            if(key == Key.LeftAlt || key == Key.RightAlt)
            {
                if(!_altNumpadEntryMode)
                {
                    _altNumpadEntryMode = true;
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;
                    handled = true;
                }
            }
            return handled;
        }

        // AltNumpad key handler
        private bool HandleAltNumpadEntry(Key key, int scanCode, bool isExtendedKey)
        {
            bool handled = false;
            Debug.Assert(_altNumpadEntryMode);

            // All Numpad keys (either numlock or not) are not an extended key. 
            // We're interested in only NumPad key so we can filter them first.
            if (isExtendedKey)
            {
                return handled;
            }

            // If Alt key is up, we will quit AltNumpadEntryMode.
            if (!Keyboard.IsKeyDown(Key.LeftAlt) && !Keyboard.IsKeyDown(Key.RightAlt))
            {
                return false;
            }
  
            //
            // Windows has historically offered a back-door for entering
            // characters that are not available on the keyboard.  The
            // user can hold down one of the Alt keys, type in the numerical
            // value of the desried character using the num-pad keys, and
            // the release the Alt key.  The numeric value will be converted
            // into Unicode, and a text event will
            // be raised.
            //

            if (scanCode == NumpadScanCode.NumpadDot)
            {
                if (IsHexNumpadEnabled)
                {
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.HexDefaultCodePage;
                    handled = true;
                }
                else
                {
                    // reset alt numpad entry and mode.
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;
                    handled = false;
                }
            }
            else if (scanCode == NumpadScanCode.NumpadPlus)
            {
                if (IsHexNumpadEnabled)
                {
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.HexUnicode;
                    handled = true;
                }
                else
                {
                    // reset alt numpad entry and mode.
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;
                    handled = false;
                }
            }
            else
            {
                int newEntry = GetNewEntry(key, scanCode);
                if (newEntry == -1)
                {
                    _altNumpadEntry = 0;
                    _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;

                    // we don't handle this case to cancel TextComposition.
                    handled = false;
                }
                else
                {
                    // If the first key is NumPad0, it is the default codepage.
                    if ((_altNumpadEntry == 0) && (newEntry == 0))
                        _altNumpadConversionMode = AltNumpadConversionMode.DefaultCodePage;

                    if (HexConversionMode)
                        _altNumpadEntry = (_altNumpadEntry * 0x10) + newEntry;
                    else
                        _altNumpadEntry = (_altNumpadEntry * 10) + newEntry;

                    handled = true;
                }
            }
            return handled;
        }

        // Convert Key and ScanCode to new entry number.
        private int GetNewEntry(Key key, int scanCode)
        {
            if (HexConversionMode)
            {
                switch (key)
                {
                    // We accept digit keys and A-F in HexConversionMode.
                    case Key.D0: return 0x00;
                    case Key.D1: return 0x01;
                    case Key.D2: return 0x02;
                    case Key.D3: return 0x03;
                    case Key.D4: return 0x04;
                    case Key.D5: return 0x05;
                    case Key.D6: return 0x06;
                    case Key.D7: return 0x07;
                    case Key.D8: return 0x08;
                    case Key.D9: return 0x09;
                    case Key.A:  return 0x0A;
                    case Key.B:  return 0x0B;
                    case Key.C:  return 0x0C;
                    case Key.D:  return 0x0D;
                    case Key.E:  return 0x0E;
                    case Key.F:  return 0x0F;
                }
            }

            return NumpadScanCode.DigitFromScanCode(scanCode);
        }

        // Convert the code to byte array for DBCS/SBCS.
        private static Byte[] ConvertCodeToByteArray(int codeEntry)
        {
            Byte[] bytes;
            if (codeEntry > 0xff)
            {
                bytes = new Byte[2];
                bytes[0] = (Byte)(codeEntry >> 8);
                bytes[1] = (Byte)codeEntry;
            }
            else
            {
                bytes = new Byte[1];
                bytes[0] = (Byte)codeEntry;
            }
            return bytes;
        }

        // clear the altnumpad composition object and reset entry.
        // the existence of the altnumpad composition object needs to be consistent with altnumpad entry.
        private void ClearAltnumpadComposition()
        {
            _altNumpadcomposition = null;
            _altNumpadConversionMode = AltNumpadConversionMode.OEMCodePage;
            _altNumpadEntry = 0;
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        // Return true if we're in hex conversion mode.
        private bool HexConversionMode
        {
            get 
            {
                if ((_altNumpadConversionMode == AltNumpadConversionMode.HexDefaultCodePage) ||
                    (_altNumpadConversionMode == AltNumpadConversionMode.HexUnicode))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Return true if HexNumPad is enabled.
        /// </summary>
        private static bool IsHexNumpadEnabled
        {
            get 
            {
                if (!_isHexNumpadRegistryChecked)
                {
                    object obj;
                    RegistryKey key;

                    key = Registry.CurrentUser.OpenSubKey("Control Panel\\Input Method");
                    if (key != null)
                    {
                        obj = key.GetValue("EnableHexNumpad");

                        if ((obj is string) && ((string)obj != "0"))
                        {
                            _isHexNumpadEnabled = true;
                        }
                    }

                    _isHexNumpadRegistryChecked = true;
                }
                return _isHexNumpadEnabled;
            }
        }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // InputManager for this TextCompositionManager
        
        private readonly InputManager _inputManager;

        // The current dead char composition.
        private DeadCharTextComposition _deadCharTextComposition;

        // The state of AltNumpad mode.
        private bool _altNumpadEntryMode;

        // The current value entered from NumPad.
        private int _altNumpadEntry;

        // The current AltNumpad conversion mode.
        private AltNumpadConversionMode _altNumpadConversionMode;

        // TextComposition for AltNumpad.
        private TextComposition _altNumpadcomposition;

        // True if EnableHexNumpad registry has been checked.
        private static bool _isHexNumpadRegistryChecked = false;

        // True if EnableHexNumpad registry is set.
        private static bool _isHexNumpadEnabled = false;

        // Character encoding length.
        private const int EncodingBufferLen  =  4;

        // ScanCode of Numpad keys.
        internal static class NumpadScanCode
        {
            internal static int DigitFromScanCode(int scanCode)
            {
                switch (scanCode)
                {
                    case Numpad0: return 0;
                    case Numpad1: return 1;
                    case Numpad2: return 2;
                    case Numpad3: return 3;
                    case Numpad4: return 4;
                    case Numpad5: return 5;
                    case Numpad6: return 6;
                    case Numpad7: return 7;
                    case Numpad8: return 8;
                    case Numpad9: return 9;
                }
                return -1;
            }
            internal const int NumpadDot      =  0x53;
            internal const int NumpadPlus     =  0x4e;
            internal const int Numpad0        =  0x52;
            internal const int Numpad1        =  0x4f;
            internal const int Numpad2        =  0x50;
            internal const int Numpad3        =  0x51;
            internal const int Numpad4        =  0x4b;
            internal const int Numpad5        =  0x4c;
            internal const int Numpad6        =  0x4d;
            internal const int Numpad7        =  0x47;
            internal const int Numpad8        =  0x48;
            internal const int Numpad9        =  0x49;
        }
    }
}
