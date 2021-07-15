// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: The information for the compartments.
//
//

using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Windows;
using MS.Internal; //TextServicesInterop
using MS.Utility;
using MS.Win32;

using System;

namespace System.Windows.Input 
{
    //------------------------------------------------------
    //
    //  InputMethodStateType enum
    //
    //------------------------------------------------------

    /// <summary>
    /// This is an internal.
    /// This enum identifies the type of input method event.
    /// </summary>
    internal enum InputMethodStateType
    {
        Invalid,
        ImeState,
        MicrophoneState,
        HandwritingState,
        SpeechMode,
        ImeConversionModeValues,
        ImeSentenceModeValues,
    }

    internal enum CompartmentScope
    {
        Invalid,
        Thread,
        Global,
    }

    //------------------------------------------------------
    //
    //  InputMethodEventTypeInfo class
    //
    //------------------------------------------------------

    /// <summary>
    /// This is an internal.
    /// This is a holder of compartment type information.
    /// </summary>
    internal class InputMethodEventTypeInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        internal InputMethodEventTypeInfo(
                                 InputMethodStateType type, 
                                 Guid guid, 
                                 CompartmentScope scope)
        {
            _inputmethodstatetype = type;
            _guid = guid;
            _scope = scope;
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        
        #region Internal Methods

        /// <summary>
        ///    This converts from GUID for ITfCompartment to InputMethodStateType.
        /// </summary>
        internal static InputMethodStateType ToType(ref Guid rguid)
        {
             for (int i = 0; i < _iminfo.Length; i++)
             {
                 InputMethodEventTypeInfo im = _iminfo[i];

                 if (rguid == im._guid)
                     return im._inputmethodstatetype;
             }

             Debug.Assert(false, "The guid does not match.");
             return InputMethodStateType.Invalid;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        #region Internal Properties

        internal InputMethodStateType Type {get{return _inputmethodstatetype;}}
        internal Guid Guid {get{return _guid;}}
        internal CompartmentScope Scope {get{return _scope;}}
        internal static InputMethodEventTypeInfo[] InfoList {get{return _iminfo;}}

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        private static readonly InputMethodEventTypeInfo _iminfoImeState = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.ImeState,
                     UnsafeNativeMethods.GUID_COMPARTMENT_KEYBOARD_OPENCLOSE,
                     CompartmentScope.Thread);

        private static readonly InputMethodEventTypeInfo _iminfoHandwritingState = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.HandwritingState,
                     UnsafeNativeMethods.GUID_COMPARTMENT_HANDWRITING_OPENCLOSE,
                     CompartmentScope.Thread);

        private static readonly InputMethodEventTypeInfo _iminfoMicrophoneState = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.MicrophoneState,
                     UnsafeNativeMethods.GUID_COMPARTMENT_SPEECH_OPENCLOSE,
                     CompartmentScope.Global);

        private static readonly InputMethodEventTypeInfo _iminfoSpeechMode = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.SpeechMode,
                     UnsafeNativeMethods.GUID_COMPARTMENT_SPEECH_GLOBALSTATE,
                     CompartmentScope.Global);

        private static readonly InputMethodEventTypeInfo _iminfoImeConversionMode = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.ImeConversionModeValues,
                     UnsafeNativeMethods.GUID_COMPARTMENT_KEYBOARD_INPUTMODE_CONVERSION,
                     CompartmentScope.Thread);

        private static readonly InputMethodEventTypeInfo _iminfoImeSentenceMode = 
             new InputMethodEventTypeInfo(
                     InputMethodStateType.ImeSentenceModeValues,
                     UnsafeNativeMethods.GUID_COMPARTMENT_KEYBOARD_INPUTMODE_SENTENCE,
                     CompartmentScope.Thread);

        private static readonly InputMethodEventTypeInfo[] _iminfo = 
            new InputMethodEventTypeInfo[] {
                    _iminfoImeState, 
                    _iminfoHandwritingState, 
                    _iminfoMicrophoneState, 
                    _iminfoSpeechMode,
                    _iminfoImeConversionMode,
                    _iminfoImeSentenceMode};

        private InputMethodStateType _inputmethodstatetype;
        private Guid _guid;
        private CompartmentScope _scope;

        #endregion Private Fields
    }
}

