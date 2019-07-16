// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Manage Input Methods (EA-IME, TextServicesFramework).
//
//

using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Security;
using MS.Utility;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper

using System;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input 
{
    //------------------------------------------------------
    //
    //  InputMethodState enum
    //
    //------------------------------------------------------

    /// <summary>
    /// State of Ime
    /// </summary>
    public enum InputMethodState
    {
        /// <summary>
        /// InputMethod state is on.
        /// </summary>
        Off = 0,

        /// <summary>
        /// InputMethod state is on.
        /// </summary>
        On  = 1,

        /// <summary>
        /// InputMethod state is not set. It does not care.
        /// </summary>
        DoNotCare  = 2,
    }

    //------------------------------------------------------
    //
    //  SpeechMode enum
    //
    //------------------------------------------------------

    /// <summary>
    /// Mode of speech
    /// </summary>
    public enum SpeechMode
    {
        /// <summary>
        /// Speech is in dictation mode.
        /// </summary>
        Dictation,

        /// <summary>
        /// Speech is in command mode.
        /// </summary>
        Command,

        /// <summary>
        /// Speech mode is indeterminate.
        /// </summary>
        Indeterminate,
}

    //------------------------------------------------------
    //
    //  ImeConversionModeValues enum
    //
    //------------------------------------------------------

    /// <summary>
    /// ImeConversionModeValues
    /// </summary>
    [Flags]
    public enum ImeConversionModeValues
    {
        /// <summary>
        /// Native Mode (Hiragana, Hangul, Chinese)
        /// </summary>
        Native            = 0x00000001,
        /// <summary>
        /// Japanese Katakana Mode
        /// </summary>
        Katakana          = 0x00000002,
        /// <summary>
        /// Full Shape mode
        /// </summary>
        FullShape         = 0x00000004,
        /// <summary>
        /// Roman Input Mode
        /// </summary>
        Roman             = 0x00000008,
        /// <summary>
        /// Roman Input Mode
        /// </summary>
        CharCode          = 0x00000010,
        /// <summary>
        /// No conversion
        /// </summary>
        NoConversion      = 0x00000020,
        /// <summary>
        /// EUDC symbol(bopomofo) Mode
        /// </summary>
        Eudc              = 0x00000040,
        /// <summary>
        /// Symbol Input Mode
        /// </summary>
        Symbol            = 0x00000080, 
        /// <summary>
        /// Fixed Input Mode
        /// </summary>
        Fixed             = 0x00000100,
        /// <summary>
        /// Alphanumeric mode (Alphanumeric mode was 0x0 in Win32 IMM/Cicero).
        /// </summary>
        Alphanumeric      = 0x00000200,

        /// <summary>
        /// Mode is not set. It does not care.
        /// </summary>
        DoNotCare         = unchecked((int)0x80000000),
    }

    //------------------------------------------------------
    //
    //  ImeSentenceModeValues enum
    //
    //------------------------------------------------------

    /// <summary>
    /// ImeSentenceModeValues
    /// </summary>
    [Flags]
    public enum ImeSentenceModeValues
    {
        /// <summary>
        /// Non Sentence conversion
        /// </summary>
        None               = 0x00000000,   
        /// <summary>
        /// PluralClause conversion
        /// </summary>
        PluralClause       = 0x00000001,      
        /// <summary>
        /// Single Kanji/Hanja conversion
        /// </summary>
        SingleConversion   = 0x00000002,   
        /// <summary>
        /// automatic conversion mode
        /// </summary>
        Automatic          = 0x00000004,    
        /// <summary>
        /// phrase prediction mode
        /// </summary>
        PhrasePrediction   = 0x00000008,   
        /// <summary>
        /// conversation style conversion mode
        /// </summary>
        Conversation       = 0x00000010, 

        /// <summary>
        /// Mode is not set. It does not care.
        /// </summary>
        DoNotCare          = unchecked((int)0x80000000),
    }
 
   
    //------------------------------------------------------
    //
    //  InputMethod class
    //
    //------------------------------------------------------

    /// <summary>
    /// The InputMethod class is a place holder for Cicero API, which are
    /// communicating or accessing TIP's properties.
    /// </summary>
    public class InputMethod : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal InputMethod()
        {
        }

        //------------------------------------------------------
        //
        //  Static Initialization 
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Static Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     A dependency property that enables alternative text inputs.
        /// </summary>
        public static readonly DependencyProperty IsInputMethodEnabledProperty =
                DependencyProperty.RegisterAttached(
                        "IsInputMethodEnabled",
                        typeof(bool),
                        typeof(InputMethod),
                        new PropertyMetadata(
                                true, 
                                new PropertyChangedCallback(IsInputMethodEnabled_Changed)));

        /// <summary>
        /// Setter for IsInputMethodEnabled DependencyProperty
        /// </summary>
        public static void SetIsInputMethodEnabled(DependencyObject target, bool value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(IsInputMethodEnabledProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodEnabled DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsInputMethodEnabled(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (bool)(target.GetValue(IsInputMethodEnabledProperty));
        }

        /// <summary>
        ///     A dependency property that suspends alternative text inputs.
        ///     If this property is true, the document focus remains in the previous focus element
        ///     and the key events won't be dispatched into Cicero/IMEs.
        /// </summary>
        public static readonly DependencyProperty IsInputMethodSuspendedProperty =
                DependencyProperty.RegisterAttached(
                        "IsInputMethodSuspended",
                        typeof(bool),
                        typeof(InputMethod),
                        new PropertyMetadata(false));

        /// <summary>
        /// Setter for IsInputMethodSuspended DependencyProperty
        /// </summary>
        public static void SetIsInputMethodSuspended(DependencyObject target, bool value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(IsInputMethodSuspendedProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodSuspended DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsInputMethodSuspended(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (bool)(target.GetValue(IsInputMethodSuspendedProperty));
        }


        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME status is changed to
        /// the preferred state (open or close)
        /// </summary>
        public static readonly DependencyProperty PreferredImeStateProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeState",
                        typeof(InputMethodState),
                        typeof(InputMethod),
                        new PropertyMetadata(InputMethodState.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeState DependencyProperty
        /// </summary>
        public static void SetPreferredImeState(DependencyObject target, InputMethodState value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(PreferredImeStateProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeState DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static InputMethodState GetPreferredImeState(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (InputMethodState)(target.GetValue(PreferredImeStateProperty));
        }
         
        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME conversion mode is changed to
        /// the preferred mode
        /// </summary>
        public static readonly DependencyProperty PreferredImeConversionModeProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeConversionMode",
                        typeof(ImeConversionModeValues),
                        typeof(InputMethod),
                        new PropertyMetadata(ImeConversionModeValues.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeConversionMode DependencyProperty
        /// </summary>
        public static void SetPreferredImeConversionMode(DependencyObject target, ImeConversionModeValues value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(PreferredImeConversionModeProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeConversionMode DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static ImeConversionModeValues GetPreferredImeConversionMode(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (ImeConversionModeValues)(target.GetValue(PreferredImeConversionModeProperty));
        }
         
        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME sentence mode is changed to
        /// the preferred mode
        /// </summary>
        public static readonly DependencyProperty PreferredImeSentenceModeProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeSentenceMode",
                        typeof(ImeSentenceModeValues),
                        typeof(InputMethod),
                        new PropertyMetadata(ImeSentenceModeValues.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeSentenceMode DependencyProperty
        /// </summary>
        public static void SetPreferredImeSentenceMode(DependencyObject target, ImeSentenceModeValues value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(PreferredImeSentenceModeProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeSentenceMode DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static ImeSentenceModeValues GetPreferredImeSentenceMode(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (ImeSentenceModeValues)(target.GetValue(PreferredImeSentenceModeProperty));
        }

        /// <summary>
        /// InputScope is the specified document context for UIElement.
        /// This is a property for UIElements such as TextBox. 
        /// </summary>
        public static readonly DependencyProperty InputScopeProperty =
                DependencyProperty.RegisterAttached(
                        "InputScope",
                        typeof(InputScope),
                        typeof(InputMethod),
                        new PropertyMetadata((InputScope) null));

        /// <summary>
        /// Setter for InputScope DependencyProperty
        /// </summary>
        public static void SetInputScope(DependencyObject target, InputScope value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(InputScopeProperty, value);
        }

        /// <summary>
        /// Getter for InputScope DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static InputScope GetInputScope(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (InputScope)(target.GetValue(InputScopeProperty));
        }

        /// <summary>
        ///     Return the input language manager associated 
        ///     with the current context.
        /// </summary>
        public static InputMethod Current
        {
            get
            {
                InputMethod inputMethod = null;
            
                // Do not auto-create the dispatcher.
                Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                if(dispatcher != null)
                {
                    inputMethod = dispatcher.InputMethod as InputMethod;
                
                    if (inputMethod == null)
                    {
                        inputMethod = new InputMethod();
                        dispatcher.InputMethod = inputMethod;
                    }
                }
                return inputMethod;
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        ///    Show the configure UI of the current active keyboard text service.
        /// </summary> 
        public void ShowConfigureUI()
        {
            ShowConfigureUI(null);
        }

        /// <summary>
        ///    Show the configure UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="element">
        ///     Specify UIElement which frame window becomes the parent of the configure UI.
        ///     This param can be null.
        /// </param>
        public void ShowConfigureUI(UIElement element)
        {
            _ShowConfigureUI(element, true);
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        public void ShowRegisterWordUI()
        {
            ShowRegisterWordUI("");
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="registeredText">
        ///     Specify default string to be registered. This is usually shown in the 
        ///     text field of the register word UI.
        /// </param>
        public void ShowRegisterWordUI(string registeredText)
        {
            ShowRegisterWordUI(null, registeredText);
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="element">
        ///     Specify UIElement which frame window becomes the parent of the configure UI.
        ///     This param can be null.
        /// </param>
        /// <param name="registeredText">
        ///     Specify default string to be registered. This is usually shown in the 
        ///     text field of the register word UI.
        /// </param>
        public void ShowRegisterWordUI(UIElement element, string registeredText)
        {
            _ShowRegisterWordUI(element, true, registeredText);
        }

        #endregion Public Methods        
 
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Access the current keyboard on/off (open/close) status.
        /// </summary> 
        public InputMethodState ImeState
        {
            get
            {
                if (!IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is not the real IMM32-IME, we get the open status from Cicero.
                    //
                    TextServicesCompartment compartment;
                    compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeState);
                    if (compartment != null)
                    {
                        return compartment.BooleanValue ? InputMethodState.On
                                                    : InputMethodState.Off;
                    }
                }
                else
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the open status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        bool fOpen = UnsafeNativeMethods.ImmGetOpenStatus(new HandleRef(this, himc));
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));

                        return fOpen ? InputMethodState.On : InputMethodState.Off;
                    }
                }
                return InputMethodState.Off;
            }

            set
            {
                Debug.Assert(value != InputMethodState.DoNotCare);

                //
                // Update Cicero's keyboard Open/Close status.
                //
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeState);
                if (compartment != null)
                {
                    // we don't have to set compartment unless the value is changed.
                    if (compartment.BooleanValue != (value == InputMethodState.On))
                    {
                        compartment.BooleanValue = (value == InputMethodState.On);
                    }
                }

                //
                // Under IMM32 enabled system, we call IMM32 API to update open status as well as Cicero
                //
                if (_immEnabled)
                {
                    IntPtr hwnd = IntPtr.Zero;
                    hwnd = HwndFromInputElement(Keyboard.FocusedElement);

                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        bool fOpen = UnsafeNativeMethods.ImmGetOpenStatus(new HandleRef(this, himc));

                        // we don't have to call IMM unless the value is changed.
                        if (fOpen != (value == InputMethodState.On))
                        {
                           UnsafeNativeMethods.ImmSetOpenStatus(new HandleRef(this, himc), (value == InputMethodState.On));
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current microphone on/off status.
        /// </summary>
        public InputMethodState MicrophoneState
        {
            get
            {
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.MicrophoneState);
                if (compartment != null)
                {
                    return compartment.BooleanValue ? InputMethodState.On
                                                    : InputMethodState.Off;
                }
                return InputMethodState.Off;
            }

            set
            {

                Debug.Assert(value != InputMethodState.DoNotCare);

                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.MicrophoneState);
                if (compartment != null)
                {
                    // we don't have to set compartment unless the value is changed.
                    if (compartment.BooleanValue != (value == InputMethodState.On))
                    {
                        compartment.BooleanValue = (value == InputMethodState.On);
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current handwriting on/off status.
        /// </summary> 
        public InputMethodState HandwritingState
        {
            get
            {
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.HandwritingState);
                if (compartment != null)
                {
                    return compartment.BooleanValue ? InputMethodState.On
                                                    : InputMethodState.Off;
                }
                return InputMethodState.Off;
            }

            set
            {
                Debug.Assert(value != InputMethodState.DoNotCare);

                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.HandwritingState);
                if (compartment != null)
                {
                    // we don't have to set compartment unless the value is changed.
                    if (compartment.BooleanValue != (value == InputMethodState.On))
                    {
                        compartment.BooleanValue = (value == InputMethodState.On);
                    }
                }
            }
        }


        /// <summary> 
        /// Access the current speech mode
        /// </summary> 
        public SpeechMode SpeechMode
        {
            get
            {
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.SpeechMode);

                if (compartment != null)
                {
                    int nValue = compartment.IntValue;
                    if ((nValue & UnsafeNativeMethods.TF_DICTATION_ON) != 0)
                        return SpeechMode.Dictation;
                    if ((nValue & UnsafeNativeMethods.TF_COMMANDING_ON) != 0)
                        return SpeechMode.Command;
                }

                return SpeechMode.Indeterminate;
            }

            set
            {

                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.SpeechMode);

                if (compartment != null)
                {
                    int nValue = compartment.IntValue;
                    if (value == SpeechMode.Dictation)
                    {
                        nValue &= ~UnsafeNativeMethods.TF_COMMANDING_ON;
                        nValue |= UnsafeNativeMethods.TF_DICTATION_ON;
                        // we don't have to set compartment unless the value is changed.
                        if (compartment.IntValue != nValue)
                        {
                            compartment.IntValue = nValue;
                        }
                    }
                    else if (value == SpeechMode.Command)
                    {
                        nValue &= ~UnsafeNativeMethods.TF_DICTATION_ON;
                        nValue |= UnsafeNativeMethods.TF_COMMANDING_ON;
                        // we don't have to set compartment unless the value is changed.
                        if (compartment.IntValue != nValue)
                        {
                            compartment.IntValue = nValue;
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "Unknown Speech Mode");
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current ime conversion mode
        /// </summary> 
        public ImeConversionModeValues ImeConversionMode
        {
            get
            {
                if (!IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is not the real IMM32-IME, we get the conversion status from Cicero.
                    //
                    TextServicesCompartment compartment;
                    compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeConversionModeValues);

                    if (compartment != null)
                    {
                        UnsafeNativeMethods.ConversionModeFlags convmode = (UnsafeNativeMethods.ConversionModeFlags)compartment.IntValue;
                        ImeConversionModeValues ret = 0;
                        if ((convmode & (UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA)) == 0)
                            ret |= ImeConversionModeValues.Alphanumeric;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE) != 0)
                            ret |= ImeConversionModeValues.Native;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA) != 0)
                            ret |= ImeConversionModeValues.Katakana;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE) != 0)
                            ret |= ImeConversionModeValues.FullShape;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_ROMAN) != 0)
                            ret |= ImeConversionModeValues.Roman;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_CHARCODE) != 0)
                            ret |= ImeConversionModeValues.CharCode;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NOCONVERSION) != 0)
                            ret |= ImeConversionModeValues.NoConversion;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_EUDC) != 0)
                            ret |= ImeConversionModeValues.Eudc;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_SYMBOL) != 0)
                            ret |= ImeConversionModeValues.Symbol;
                        if ((convmode & UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FIXED) != 0)
                            ret |= ImeConversionModeValues.Fixed;

                        return ret;
                    }
                }
                else
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the conversion status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));


                        ImeConversionModeValues ret = 0;
                        if ((convmode & (NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_KATAKANA)) == 0)
                            ret |= ImeConversionModeValues.Alphanumeric;
                        if ((convmode & NativeMethods.IME_CMODE_NATIVE) != 0)
                            ret |= ImeConversionModeValues.Native;
                        if ((convmode & NativeMethods.IME_CMODE_KATAKANA) != 0)
                            ret |= ImeConversionModeValues.Katakana;
                        if ((convmode & NativeMethods.IME_CMODE_FULLSHAPE) != 0)
                            ret |= ImeConversionModeValues.FullShape;
                        if ((convmode & NativeMethods.IME_CMODE_ROMAN) != 0)
                            ret |= ImeConversionModeValues.Roman;
                        if ((convmode & NativeMethods.IME_CMODE_CHARCODE) != 0)
                            ret |= ImeConversionModeValues.CharCode;
                        if ((convmode & NativeMethods.IME_CMODE_NOCONVERSION) != 0)
                            ret |= ImeConversionModeValues.NoConversion;
                        if ((convmode & NativeMethods.IME_CMODE_EUDC) != 0)
                            ret |= ImeConversionModeValues.Eudc;
                        if ((convmode & NativeMethods.IME_CMODE_SYMBOL) != 0)
                            ret |= ImeConversionModeValues.Symbol;
                        if ((convmode & NativeMethods.IME_CMODE_FIXED) != 0)
                            ret |= ImeConversionModeValues.Fixed;

                        return ret;
                    }
                }

                return ImeConversionModeValues.Alphanumeric;
            }

            set
            {
                if (!IsValidConversionMode(value))
                {
                    throw new ArgumentException(SR.Get(SRID.InputMethod_InvalidConversionMode, value));
                }

                Debug.Assert((value & ImeConversionModeValues.DoNotCare) == 0);

                IntPtr hwnd = IntPtr.Zero;
                if (_immEnabled)
                {
                    hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                }


                //
                // Update Cicero's conversion mode.
                //
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeConversionModeValues);

                if (compartment != null)
                {
                    UnsafeNativeMethods.ConversionModeFlags currentConvMode;
                    if (_immEnabled)
                    {
                        currentConvMode = Imm32ConversionModeToTSFConversionMode(hwnd);
                    }
                    else
                    {
                        currentConvMode = (UnsafeNativeMethods.ConversionModeFlags)compartment.IntValue;
                    }

                    UnsafeNativeMethods.ConversionModeFlags convmode = 0;

                    // TF_CONVERSIONMODE_ALPHANUMERIC is 0.
                    // if ((value & ImeConversionModeValues.Alphanumeric) != 0)
                    //     convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_ALPHANUMERIC;
                    if ((value & ImeConversionModeValues.Native) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE;
                    if ((value & ImeConversionModeValues.Katakana) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA;
                    if ((value & ImeConversionModeValues.FullShape) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE;
                    if ((value & ImeConversionModeValues.Roman) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_ROMAN;
                    if ((value & ImeConversionModeValues.CharCode) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_CHARCODE;
                    if ((value & ImeConversionModeValues.NoConversion) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NOCONVERSION;
                    if ((value & ImeConversionModeValues.Eudc) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_EUDC;
                    if ((value & ImeConversionModeValues.Symbol) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_SYMBOL;
                    if ((value & ImeConversionModeValues.Fixed) != 0)
                        convmode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FIXED;

                    // We don't have to set the value unless the value is changed.
                    if (currentConvMode != convmode)
                    {
                        UnsafeNativeMethods.ConversionModeFlags conversionModeClearBit = 0;

                        if (convmode == (UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE))
                        {
                            // Chinese, Hiragana or Korean so clear Katakana
                            conversionModeClearBit = UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA;
                        }
                        else if (convmode == (UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE))
                        {
                            // Katakana Half
                            conversionModeClearBit = UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE;
                        }
                        else if (convmode == UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE)
                        {
                            // Alpha Full
                            conversionModeClearBit = UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE;
                        }
                        else if (convmode == UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_ALPHANUMERIC)
                        {
                            // Alpha Half
                            conversionModeClearBit = UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA | UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE;
                        }
                        else if (convmode == UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE)
                        {
                            // Hangul
                            conversionModeClearBit = UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE;
                        }

                        // Set the new conversion mode bit and apply the clear bit
                        convmode |= currentConvMode;
                        convmode &= ~conversionModeClearBit;

                        compartment.IntValue = (int)convmode;
                    }
                }

                //
                // Under IMM32 enabled system, we call IMM32 API to update conversion status as well as Cicero
                //
                if (_immEnabled)
                {
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);

                        int convmodeNew = 0;
                        // IME_CMODE_ALPHANUMERIC is 0.
                        // if ((value & ImeConversionModeValues.Alphanumeric) != 0)
                        //     convmodeNew |= NativeMethods.IME_CMODE_ALPHANUMERIC;
                        if ((value & ImeConversionModeValues.Native) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_NATIVE;
                        if ((value & ImeConversionModeValues.Katakana) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_KATAKANA;
                        if ((value & ImeConversionModeValues.FullShape) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_FULLSHAPE;
                        if ((value & ImeConversionModeValues.Roman) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_ROMAN;
                        if ((value & ImeConversionModeValues.CharCode) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_CHARCODE;
                        if ((value & ImeConversionModeValues.NoConversion) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_NOCONVERSION;
                        if ((value & ImeConversionModeValues.Eudc) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_EUDC;
                        if ((value & ImeConversionModeValues.Symbol) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_SYMBOL;
                        if ((value & ImeConversionModeValues.Fixed) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_FIXED;

                        // We don't have to call IMM unless the value is changed.
                        if (convmode != convmodeNew)
                        {
                            int conversionModeClearBit = 0;

                            if (convmodeNew == (NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_FULLSHAPE))
                            {
                                // Chinese, Hiragana or Korean so clear Katakana
                                conversionModeClearBit = NativeMethods.IME_CMODE_KATAKANA;
                            }
                            else if (convmodeNew == (NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE))
                            {
                                // Katakana Half
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_FULLSHAPE)
                            {
                                // Alpha Full
                                conversionModeClearBit = NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_ALPHANUMERIC)
                            {
                                // Alpha Half
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE | NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_NATIVE)
                            {
                                // Hangul
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE;
                            }

                            // Set the new conversion mode bit and apply the clear bit
                            convmodeNew |= convmode;
                            convmodeNew &= ~conversionModeClearBit;

                            UnsafeNativeMethods.ImmSetConversionStatus(new HandleRef(this, himc), convmodeNew, sentence);
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current ime sentence mode
        /// </summary> 
        public ImeSentenceModeValues ImeSentenceMode
        {
            get
            {
                if (!IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is not the real IMM32-IME, we get the sentence status from Cicero.
                    //
                    TextServicesCompartment compartment;
                    compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeSentenceModeValues);

                    if (compartment != null)
                    {
                        UnsafeNativeMethods.SentenceModeFlags convmode = (UnsafeNativeMethods.SentenceModeFlags)compartment.IntValue;
                        ImeSentenceModeValues ret = 0;
    
                        // TF_SENTENCEMODE_ALPHANUMERIC is 0. 
                        if (convmode == UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_NONE)
                            return ImeSentenceModeValues.None;
    
                        if ((convmode & UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_PLAURALCLAUSE) != 0)
                            ret |= ImeSentenceModeValues.PluralClause;
                        if ((convmode & UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_SINGLECONVERT) != 0)
                            ret |= ImeSentenceModeValues.SingleConversion;
                        if ((convmode & UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_AUTOMATIC) != 0)
                            ret |= ImeSentenceModeValues.Automatic;
                        if ((convmode & UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_PHRASEPREDICT) != 0)
                            ret |= ImeSentenceModeValues.PhrasePrediction;
                        if ((convmode & UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_CONVERSATION) != 0)
                            ret |= ImeSentenceModeValues.Conversation;
    
                        return ret;
                    }
                }
                else
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the sentence status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        ImeSentenceModeValues ret = 0;
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));


                        // TF_SENTENCEMODE_ALPHANUMERIC is 0. 
                        if (sentence == NativeMethods.IME_SMODE_NONE)
                            return ImeSentenceModeValues.None;
    
                        if ((sentence & NativeMethods.IME_SMODE_PLAURALCLAUSE) != 0)
                            ret |= ImeSentenceModeValues.PluralClause;
                        if ((sentence & NativeMethods.IME_SMODE_SINGLECONVERT) != 0)
                            ret |= ImeSentenceModeValues.SingleConversion;
                        if ((sentence & NativeMethods.IME_SMODE_AUTOMATIC) != 0)
                            ret |= ImeSentenceModeValues.Automatic;
                        if ((sentence & NativeMethods.IME_SMODE_PHRASEPREDICT) != 0)
                            ret |= ImeSentenceModeValues.PhrasePrediction;
                        if ((sentence & NativeMethods.IME_SMODE_CONVERSATION) != 0)
                            ret |= ImeSentenceModeValues.Conversation;
    
                        return ret;
                    }
                }
                return ImeSentenceModeValues.None;
            }

            set
            {
                if (!IsValidSentenceMode(value))
                {
                    throw new ArgumentException(SR.Get(SRID.InputMethod_InvalidSentenceMode, value));
                }

                Debug.Assert((value & ImeSentenceModeValues.DoNotCare) == 0);

                //
                // Update Cicero's sentence mode.
                //
                TextServicesCompartment compartment;
                compartment = TextServicesCompartmentContext.Current.GetCompartment(InputMethodStateType.ImeSentenceModeValues);

                if (compartment != null)
                {
                    UnsafeNativeMethods.SentenceModeFlags convmode = 0;

                    if ((value & ImeSentenceModeValues.PluralClause) != 0)
                        convmode |= UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_PLAURALCLAUSE;
                    if ((value & ImeSentenceModeValues.SingleConversion) != 0)
                        convmode |= UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_SINGLECONVERT;
                    if ((value & ImeSentenceModeValues.Automatic) != 0)
                        convmode |= UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_AUTOMATIC;
                    if ((value & ImeSentenceModeValues.PhrasePrediction) != 0)
                        convmode |= UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_PHRASEPREDICT;
                    if ((value & ImeSentenceModeValues.Conversation) != 0)
                        convmode |= UnsafeNativeMethods.SentenceModeFlags.TF_SENTENCEMODE_CONVERSATION;

                    // We don't have to set the value unless the value is changed.
                    if (compartment.IntValue != (int)convmode)
                    {
                        compartment.IntValue = (int)convmode;
                    }
                }

                //
                // Under IMM32 enabled system, we call IMM32 API to update sentence status as well as Cicero
                //
                if (_immEnabled)
                {
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        int sentenceNew = 0;

                        if ((value & ImeSentenceModeValues.PluralClause) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_PLAURALCLAUSE;
                        if ((value & ImeSentenceModeValues.SingleConversion) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_SINGLECONVERT;
                        if ((value & ImeSentenceModeValues.Automatic) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_AUTOMATIC;
                        if ((value & ImeSentenceModeValues.PhrasePrediction) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_PHRASEPREDICT;
                        if ((value & ImeSentenceModeValues.Conversation) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_CONVERSATION;

                        // We don't have to call IMM unless the value is changed.
                        if (sentence != sentenceNew)
                        {
                            UnsafeNativeMethods.ImmSetConversionStatus(new HandleRef(this, himc), convmode, sentenceNew);
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
}
        }

        /// <summary> 
        ///    This is a property that indicates if the current keybaord text services
        ///    can show the configure UI.
        /// </summary> 
        public bool CanShowConfigurationUI
        {
            get
            {
                return _ShowConfigureUI(null, false);
            }
        }

        /// <summary> 
        ///    This is a property that indicates if the current keybaord text services
        ///    can show the register word UI.
        /// </summary> 
        public bool CanShowRegisterWordUI
        {
            get
            {
                return _ShowRegisterWordUI(null, false, "");
            }
        }

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        ///     An event for input method state changed.
        /// </summary>
        public event InputMethodStateChangedEventHandler StateChanged
        {
            add
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // Advise compartment event sink to Win32 Cicero only when someone
                // has StateChanged event handler.
                if ((_StateChanged == null) && TextServicesLoader.ServicesInstalled)
                {
                    InitializeCompartmentEventSink();
                }

                _StateChanged += value;
            }
            remove
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _StateChanged -= value;
                if ((_StateChanged == null) && TextServicesLoader.ServicesInstalled)
                {
                    // Unadvise compartment event sink to Win32 Cicero if none has StateChanged event handler.
                    UninitializeCompartmentEventSink();
                }
            }
        }



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
 
        #region Internal Methods

        /// <summary>
        ///     When keyboard device gets focus, this is called.
        ///     We will check the preferred input statues of focus element.
        ///     The preferred input methods should be applied after Cicero TIP gots SetFocus callback.
        /// </summary> 
        internal void GotKeyboardFocus(DependencyObject focus)
        {
            object value;

            if (focus == null)
                return;

            //
            // Check the InputLanguageProperty of the focus element.
            //
            value = focus.GetValue(PreferredImeStateProperty);
            if ((value != null) && ((InputMethodState)value != InputMethodState.DoNotCare))
            {
                ImeState = (InputMethodState)value;
            }

            value = focus.GetValue(PreferredImeConversionModeProperty);
            if ((value != null) && (((ImeConversionModeValues)value & ImeConversionModeValues.DoNotCare) == 0))
            {
                ImeConversionMode = (ImeConversionModeValues)value;
            }

            value = focus.GetValue(PreferredImeSentenceModeProperty);
            if ((value != null) && (((ImeSentenceModeValues)value & ImeSentenceModeValues.DoNotCare) == 0))
            {
                ImeSentenceMode = (ImeSentenceModeValues)value;
            }
        }

        /// <summary>
        ///    TextServicesCompartmentEventSink forwards OnChange evennt here.
        /// </summary> 
        internal void OnChange(ref Guid rguid)
        {
            if (_StateChanged != null)
            {
                InputMethodStateType imtype = InputMethodEventTypeInfo.ToType(ref rguid);

                // Stability Review: Task#32415
                //   - No state to be restored even exception happens while this callback.
                _StateChanged(this, new InputMethodStateChangedEventArgs(imtype));
            }
        }

        /// <summary>
        /// return true if the current keyboard layout is a real IMM32-IME.
        /// </summary>
        internal static bool IsImm32ImeCurrent()
        {
            if (!_immEnabled)
            {
                return false;
            }

            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            return IsImm32Ime(hkl);
        }

        /// <summary>
        /// return true if the keyboard layout is a real IMM32-IME.
        /// </summary>
        internal static bool IsImm32Ime(IntPtr hkl)
        {
            if (hkl == IntPtr.Zero)
            {
                return false;
            }

            return ((NativeMethods.IntPtrToInt32(hkl) & 0xf0000000) == 0xe0000000);
        }

        /// <summary>
        ///     This is call back for the IsInputMethodEnable property.
        /// </summary> 
        private static void IsInputMethodEnabled_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IInputElement inputElement = (IInputElement)d;
            if (inputElement == Keyboard.FocusedElement)
            {
                InputMethod.Current.EnableOrDisableInputMethod((bool) e.NewValue);
            }
        }

        /// <summary>
        ///     InputMethod enabling/disabling function.
        ///     This takes care of both Cicero and IMM32.
        /// </summary> 
        internal void EnableOrDisableInputMethod(bool bEnabled)
        {
            // InputMethod enable/disabled status was changed on the current focus Element.
            if (TextServicesLoader.ServicesInstalled &&
                TextServicesContext.DispatcherCurrent != null)
            {
                if (bEnabled)
                {
                    // Enabled. SetFocus to the default text store.
                    TextServicesContext.DispatcherCurrent.SetFocusOnDefaultTextStore();
                }
                else
                {
                    // Disabled. SetFocus to the empty dim.
                    TextServicesContext.DispatcherCurrent.SetFocusOnEmptyDim();
                }
            }

            //
            // Under IMM32 enabled system, we associate default hIMC or null hIMC.
            //
            if (_immEnabled)
            {
                IntPtr hwnd;
                hwnd = HwndFromInputElement(Keyboard.FocusedElement);
 
                if (bEnabled)
                {
                    //
                    // Enabled. Use the default hIMC.
                    //
                    if (DefaultImc != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.ImmAssociateContext(new HandleRef(this, hwnd), new HandleRef(this, _defaultImc.Value));
                    }
                }
                else 
                {
                    //
                    // Disable. Use null hIMC.
                    //
                    UnsafeNativeMethods.ImmAssociateContext(new HandleRef(this, hwnd), new HandleRef(this, IntPtr.Zero));
                }
}
        }
 
        #endregion Internal methods
        
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        #region Internal Properties

        // Set and get the referrence of TextServicesCompartmentContext for
        // the Dispatcher's dispatcher thread.
        internal TextServicesContext TextServicesContext
        {
            get {return _textservicesContext;}
            set {_textservicesContext = value;}
        }

        // Set and get the per Dispatcher cache of TextServicesCompartmentContext
        internal TextServicesCompartmentContext TextServicesCompartmentContext
        {
            get {return _textservicesCompartmentContext;}
            set {_textservicesCompartmentContext = value;}
        }

        // Set and get the per Dispatcher cache of InputLanguageManager
        internal InputLanguageManager InputLanguageManager
        {
            get {return _inputlanguagemanager;}
            set {_inputlanguagemanager = value;}
        }

        // Set and get the per Dispatcher cache of DefaultTextStore
        internal DefaultTextStore DefaultTextStore
        {
            get {return _defaulttextstore;}
            set {_defaulttextstore = value;}
        }

        #endregion Internal Properties
 
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Converts Imm32 conversion mode values into TSF conversion mode values.
        /// </summary>
        /// <returns></returns>
        private UnsafeNativeMethods.ConversionModeFlags Imm32ConversionModeToTSFConversionMode(IntPtr hwnd)
        {
            UnsafeNativeMethods.ConversionModeFlags convMode = 0;
            if (hwnd != IntPtr.Zero)
            {
                int immConvMode = 0;
                int sentence = 0;
                IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref immConvMode, ref sentence);
                UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));

                if ((immConvMode & NativeMethods.IME_CMODE_NATIVE) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NATIVE;
                if ((immConvMode & NativeMethods.IME_CMODE_KATAKANA) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_KATAKANA;
                if ((immConvMode & NativeMethods.IME_CMODE_FULLSHAPE) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FULLSHAPE;
                if ((immConvMode & NativeMethods.IME_CMODE_ROMAN) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_ROMAN;
                if ((immConvMode & NativeMethods.IME_CMODE_CHARCODE) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_CHARCODE;
                if ((immConvMode & NativeMethods.IME_CMODE_NOCONVERSION) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_NOCONVERSION;
                if ((immConvMode & NativeMethods.IME_CMODE_EUDC) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_EUDC;
                if ((immConvMode & NativeMethods.IME_CMODE_SYMBOL) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_SYMBOL;
                if ((immConvMode & NativeMethods.IME_CMODE_FIXED) != 0)
                    convMode |= UnsafeNativeMethods.ConversionModeFlags.TF_CONVERSIONMODE_FIXED;
            }            
            return convMode;
        }

        /// <summary>
        ///     Initialize the sink for compartments
        ///     Advice event sink to Cicero's compartment so we can get the notification
        ///     of the compartment change.
        /// </summary> 
        private void InitializeCompartmentEventSink()
        {
            for (int i = 0; i < InputMethodEventTypeInfo.InfoList.Length; i++)
            {
                InputMethodEventTypeInfo iminfo = InputMethodEventTypeInfo.InfoList[i];

                TextServicesCompartment compartment = null;
                if (iminfo.Scope == CompartmentScope.Thread)
                    compartment = TextServicesCompartmentContext.Current.GetThreadCompartment(iminfo.Guid);
                else if (iminfo.Scope == CompartmentScope.Global)
                    compartment = TextServicesCompartmentContext.Current.GetGlobalCompartment(iminfo.Guid);
                if (compartment != null)
                {
                    if (_sink == null)
                        _sink = new TextServicesCompartmentEventSink(this);
                    compartment.AdviseNotifySink(_sink);
                }
            }
        }

        /// <summary>
        ///     Uninitialize the sink for compartments
        ///     Unadvise the cicero's compartment event sink.
        /// </summary> 
        private void UninitializeCompartmentEventSink()
        {
            for (int i = 0; i < InputMethodEventTypeInfo.InfoList.Length; i++)
            {
                InputMethodEventTypeInfo iminfo = InputMethodEventTypeInfo.InfoList[i];
                TextServicesCompartment compartment = null;
                if (iminfo.Scope == CompartmentScope.Thread)
                    compartment = TextServicesCompartmentContext.Current.GetThreadCompartment(iminfo.Guid);
                else if (iminfo.Scope == CompartmentScope.Global)
                    compartment = TextServicesCompartmentContext.Current.GetGlobalCompartment(iminfo.Guid);
                if (compartment != null)
                   compartment.UnadviseNotifySink();
            }
        }

        /// <summary>
        ///    Get ITfFnConfigure interface and call Show() method.
        ///    If there is no function provider in the current keyboard TIP or the keyboard TIP does
        ///    not have ITfFnConfigure, this returns false.
        /// </summary> 
        private bool _ShowConfigureUI(UIElement element, bool fShow)
        {

            bool bCanShown = false;
            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            if (!IsImm32Ime(hkl))
            {
                UnsafeNativeMethods.TF_LANGUAGEPROFILE tf_profile;
                UnsafeNativeMethods.ITfFunctionProvider funcPrv = GetFunctionPrvForCurrentKeyboardTIP(out tf_profile);
                if (funcPrv != null)
                {
                    UnsafeNativeMethods.ITfFnConfigure fnConfigure;

                    // Readonly fields can not be passed ref to the interface methods.
                    // Create pads for them.
                    Guid iidFn = UnsafeNativeMethods.IID_ITfFnConfigure;
                    Guid guidNull = UnsafeNativeMethods.Guid_Null;

                    object obj;
                    funcPrv.GetFunction(ref guidNull, ref iidFn, out obj);
                    fnConfigure = obj as UnsafeNativeMethods.ITfFnConfigure;
                    if (fnConfigure != null)
                    {
                        // We could get ITfFnConfigure, we can say the configure UI can be shown.
                        bCanShown  = true;
                        if (fShow)
                        {
                            fnConfigure.Show(HwndFromInputElement(element), tf_profile.langid, ref tf_profile.guidProfile);
                        }
                        Marshal.ReleaseComObject(fnConfigure);
                    }
                
                    Marshal.ReleaseComObject(funcPrv);
                }
            }
            else
            {
                // There is no API to test if IMM32-IME can show the configure UI. We assume they can do.
                bCanShown  = true;
                if (fShow)
                {
                    UnsafeNativeMethods.ImmConfigureIME(new HandleRef(this, hkl), new HandleRef(this, HwndFromInputElement(element)), NativeMethods.IME_CONFIG_GENERAL, IntPtr.Zero);
                }
            }
            return bCanShown;
        }

        /// <summary>
        ///    Get ITfFnConfigureRegisterWord interface and call Show() method.
        ///    If there is no function provider in the current keyboard TIP or the keyboard TIP does
        ///    not have ITfFnConfigureRegisterWord, this returns false.
        /// </summary> 
        private bool _ShowRegisterWordUI(UIElement element, bool fShow, string strRegister)
        {

            bool bCanShown = false;
            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            if (!IsImm32Ime(hkl))
            {
                UnsafeNativeMethods.TF_LANGUAGEPROFILE tf_profile;
                UnsafeNativeMethods.ITfFunctionProvider funcPrv = GetFunctionPrvForCurrentKeyboardTIP(out tf_profile);
                if (funcPrv != null)
                {
                    UnsafeNativeMethods.ITfFnConfigureRegisterWord fnConfigure;

                    // Readonly fields can not be passed ref to the interface methods.
                    // Create pads for them.
                    Guid iidFn = UnsafeNativeMethods.IID_ITfFnConfigureRegisterWord;
                    Guid guidNull = UnsafeNativeMethods.Guid_Null;

                    object obj;
                    funcPrv.GetFunction(ref guidNull, ref iidFn, out obj);
                    fnConfigure = obj as UnsafeNativeMethods.ITfFnConfigureRegisterWord;
                    if (fnConfigure != null)
                    {
                        // We could get ITfFnConfigureRegisterWord, we can say the configure UI can be shown.
                        bCanShown  = true;
                        if (fShow)
                        {
                            fnConfigure.Show(HwndFromInputElement(element), tf_profile.langid, ref tf_profile.guidProfile, strRegister);
                        }
                        Marshal.ReleaseComObject(fnConfigure);
                    }
                
                    Marshal.ReleaseComObject(funcPrv);
                }
            }
            else
            {
                // There is no API to test if IMM32-IME can show the configure UI. We assume they can do.
                bCanShown  = true;
                if (fShow)
                {
                    NativeMethods.REGISTERWORD regWord = new NativeMethods.REGISTERWORD();
                    regWord.lpReading = null;
                    regWord.lpWord = strRegister;
                    UnsafeNativeMethods.ImmConfigureIME(new HandleRef(this, hkl), new HandleRef(this, HwndFromInputElement(element)), NativeMethods.IME_CONFIG_REGISTERWORD, ref regWord);
                }
            }
            return bCanShown;
        }

        /// <summary>
        ///    Get hwnd handle value as IntPtr from UIElement.
        /// </summary> 
        private static IntPtr HwndFromInputElement(IInputElement element)
        {
            IntPtr hwnd = (IntPtr)0;
            // We allow null element.
            if (element != null)
            {
                DependencyObject o = element as DependencyObject;
                if (o != null)
                {
                    DependencyObject containingVisual = InputElement.GetContainingVisual(o);
                    if(containingVisual != null)
                    {
                        IWin32Window win32Window = null;
                        PresentationSource source = PresentationSource.CriticalFromVisual(containingVisual);
                        if (source != null)
                        {
                            win32Window = source as IWin32Window;
                            if (win32Window != null)
                            {
                                hwnd = win32Window.Handle;
                            }
                        }
}
                }
            }

            return hwnd;
        }

        /// <summary>
        ///    Get ITfFunctionProvider of the current active keyboard TIP.
        /// </summary> 
        private UnsafeNativeMethods.ITfFunctionProvider GetFunctionPrvForCurrentKeyboardTIP(out UnsafeNativeMethods.TF_LANGUAGEPROFILE tf_profile)
        {
            // Get the profile info structre of the current active keyboard TIP.
            tf_profile = GetCurrentKeybordTipProfile();

            // Is tf_profile.clsid is Guid_Null, this is not Cicero TIP. No Function Provider.
            if (tf_profile.clsid.Equals(UnsafeNativeMethods.Guid_Null))
            {
                return null;
            }

            UnsafeNativeMethods.ITfFunctionProvider functionPrv;

            // ThreadMgr Method call will be marshalled to the dispatcher thread since Ciecro is STA.
            // We release Dispatcher while sink call back.
            // This thread doesn't have to acceess UIContex until it returns.
            TextServicesContext textservicesContext = TextServicesContext.DispatcherCurrent;
            textservicesContext.ThreadManager.GetFunctionProvider(ref tf_profile.clsid, out functionPrv);

            return functionPrv;
        }

        /// <summary>
        ///    Return the profile info structre of the current active keyboard TIP.
        ///    This enumelates all TIP's profiles and find the active keyboard category TIP.
        /// </summary> 
        private UnsafeNativeMethods.TF_LANGUAGEPROFILE GetCurrentKeybordTipProfile()
        {
            UnsafeNativeMethods.ITfInputProcessorProfiles ipp = InputProcessorProfilesLoader.Load();
            UnsafeNativeMethods.TF_LANGUAGEPROFILE tf_profile = new UnsafeNativeMethods.TF_LANGUAGEPROFILE();

            if (ipp != null)
            {
                CultureInfo inputLang = InputLanguageManager.Current.CurrentInputLanguage;
                UnsafeNativeMethods.IEnumTfLanguageProfiles enumIpp;
                ipp.EnumLanguageProfiles((short)(inputLang.LCID), out enumIpp);
                UnsafeNativeMethods.TF_LANGUAGEPROFILE[] tf_profiles = new UnsafeNativeMethods.TF_LANGUAGEPROFILE[1];

                int fetched;
                while(enumIpp.Next(1, tf_profiles,  out fetched) == NativeMethods.S_OK)
                {
                    // Check if this profile is active.
                    if (tf_profiles[0].fActive == true)
                    {
                        // Check if this profile is keyboard category..
                        if (tf_profiles[0].catid.Equals(UnsafeNativeMethods.GUID_TFCAT_TIP_KEYBOARD))
                        {
                            tf_profile = tf_profiles[0];
                            break;
                        }
                    }
                }

                Marshal.ReleaseComObject(enumIpp);
            }

            return tf_profile;
        }

        // This validates the ImeConversionMode value.
        private bool IsValidConversionMode(ImeConversionModeValues mode)
        {
            int mask = (int)(ImeConversionModeValues.Alphanumeric |
                             ImeConversionModeValues.Native       |
                             ImeConversionModeValues.Katakana     |
                             ImeConversionModeValues.FullShape    |
                             ImeConversionModeValues.Roman        |
                             ImeConversionModeValues.CharCode     |
                             ImeConversionModeValues.NoConversion |
                             ImeConversionModeValues.Eudc         |
                             ImeConversionModeValues.Symbol       |
                             ImeConversionModeValues.Fixed        |
                             ImeConversionModeValues.DoNotCare);

           if (((int)mode & ~mask) != 0)
               return false;

           return true;
        }

        // This validates the ImeSentenceMode value.
        private bool IsValidSentenceMode(ImeSentenceModeValues mode)
        {
            int mask = (int)(ImeSentenceModeValues.None              |
                             ImeSentenceModeValues.PluralClause      |
                             ImeSentenceModeValues.SingleConversion  |
                             ImeSentenceModeValues.Automatic         |
                             ImeSentenceModeValues.PhrasePrediction  |
                             ImeSentenceModeValues.Conversation      |
                             ImeSentenceModeValues.DoNotCare);

           if (((int)mode & ~mask) != 0)
               return false;

           return true;
        }


        //------------------------------------------------------
        //
        //  Private Event
        //
        //------------------------------------------------------
                
        private event InputMethodStateChangedEventHandler _StateChanged;

        //------------------------------------------------------
        //
        //  Static Private Properties
        //
        //------------------------------------------------------

        private IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc==null)
                {
                    // 
                    //  Get the default HIMC from default IME window.
                    // 
                    IntPtr hwnd = UnsafeNativeMethods.ImmGetDefaultIMEWnd(new HandleRef(this, IntPtr.Zero));
                    IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));

                    // Store the default imc to _defaultImc.
                    _defaultImc = new SecurityCriticalDataClass<IntPtr>(himc);

                    UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                }
                return _defaultImc.Value;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        // This is a slot to keep the implementaion of ITfCompartmentEventSink.
        private TextServicesCompartmentEventSink _sink;

        // Per Dispatcher Cache for TextServicesContext
        // The instance of TextServiesContext is per dispather thread.
        // But we put a reference here so that the current Dispatcher can access TextServicesContext.
        private TextServicesContext _textservicesContext;

        // Per Dispatcher Cache for TextServicesCompartmentContext
        private TextServicesCompartmentContext _textservicesCompartmentContext;

        // Per Dispatcher Cache for InputLanguageManager
        private InputLanguageManager _inputlanguagemanager;

        // Per Dispatcher Cache for DefaultTextStore
        private DefaultTextStore _defaulttextstore;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled ; 

        // the default imc. The default imc is per thread and we cache it in ThreadStatic.
        [ThreadStatic]
        private static SecurityCriticalDataClass<IntPtr> _defaultImc;

        #endregion Private Fields
    }


    //------------------------------------------------------
    //
    //  InputMethodStateChangedEventHandler delegate
    //
    //------------------------------------------------------
 
    /// <summary>
    ///     The delegate to use for handlers that receive
    ///     input method state changed event.
    /// </summary>
    public delegate void InputMethodStateChangedEventHandler(Object sender, InputMethodStateChangedEventArgs e);
}

