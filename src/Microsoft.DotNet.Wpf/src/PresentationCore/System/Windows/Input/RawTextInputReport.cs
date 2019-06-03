// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The RawTextInputReport class encapsulates the raw text input 
    ///     provided.
    /// </summary>
    /// <remarks>
    ///     It is important to note that the InputReport class only contains
    ///     blittable types.  This is required so that the report can be
    ///     marshalled across application domains.
    /// </remarks>
    internal class RawTextInputReport : InputReport
    {
        /// <summary>
        ///     Constructs ad instance of the RawKeyboardInputReport class.
        /// </summary>
        /// <param name="inputSource">
        ///     The input source that provided this input.
        /// </param>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="isDeadCharacter">
        ///     True if the char code is a dead char.
        /// </param>
        /// <param name="isSystemCharacter">
        ///     True if the char code is a system char.
        /// </param>
        /// <param name="isControlCharacter">
        ///     True if the char code is a control char.
        /// </param>
        /// <param name="characterCode">
        ///     The character code.
        /// </param>
        public RawTextInputReport(
            PresentationSource inputSource,
            InputMode mode,
            int timestamp, 
            bool isDeadCharacter,
            bool isSystemCharacter,
            bool isControlCharacter,
            char characterCode) : base(inputSource, InputType.Text, mode, timestamp)
        {
            _isDeadCharacter = isDeadCharacter;
            _isSystemCharacter = isSystemCharacter;
            _isControlCharacter = isControlCharacter;

            _characterCode = characterCode;
        }


        /// <summary>
        ///     Read-only access to the state of dead character
        /// </summary>
        public bool IsDeadCharacter {get {return _isDeadCharacter;}}

        /// <summary>
        ///     Read-only access to the state of system character
        /// </summary>
        public bool IsSystemCharacter {get {return _isSystemCharacter;}}

        /// <summary>
        ///     Read-only access to the state of control character
        /// </summary>
        public bool IsControlCharacter {get {return _isControlCharacter;}}

        /// <summary>
        ///     Read-only access to the character code that was reported.
        /// </summary>
        public char CharacterCode {get {return _characterCode;}}

        private readonly bool _isDeadCharacter;
        private readonly bool _isSystemCharacter;
        private readonly bool _isControlCharacter;
        private readonly char _characterCode;
    }    
}

