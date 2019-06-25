// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Manage Input Method.
//
//

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Windows;
using MS.Internal; //TextServicesInterop
using MS.Utility;
using MS.Win32;

namespace System.Windows.Input 
{
    //------------------------------------------------------
    //
    //  InputMethodStateChjangedEventArgs class
    //
    //------------------------------------------------------
 
    /// <summary>
    /// This InputMethodStateChangedEventArgs class is 
    /// </summary>
    public class InputMethodStateChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        internal InputMethodStateChangedEventArgs(InputMethodStateType statetype)
        {
            _statetype = statetype;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        /// <summary>
        /// IME (open/close) state is changed.
        /// </summary>
        public bool IsImeStateChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.ImeState); 
            }
        }

        /// <summary>
        /// Microphone state is changed.
        /// </summary>
        public bool IsMicrophoneStateChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.MicrophoneState); 
            }
        }

        /// <summary>
        /// Handwriting state is changed.
        /// </summary>
        public bool IsHandwritingStateChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.HandwritingState); 
            }
        }

        /// <summary>
        /// SpeechMode state is changed.
        /// </summary>
        public bool IsSpeechModeChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.SpeechMode); 
            }
        }

        /// <summary>
        /// ImeConversionMode state is changed.
        /// </summary>
        public bool IsImeConversionModeChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.ImeConversionModeValues); 
            }
        }

        /// <summary>
        /// ImeSentenceMode state is changed.
        /// </summary>
        public bool IsImeSentenceModeChanged
        {
            get  
            { 
                return (_statetype == InputMethodStateType.ImeSentenceModeValues); 
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        private InputMethodStateType _statetype;

        #endregion Private Fields
    }
}
