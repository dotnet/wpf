// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using MS.Internal;
using MS.Win32;
using System.Windows;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The RawKeyboardInputReport class encapsulates the raw input 
    ///     provided from a keyboard.
    /// </summary>
    /// <remarks>
    ///     It is important to note that the InputReport class only contains
    ///     blittable types.  This is required so that the report can be
    ///     marshalled across application domains.
    /// </remarks>
    internal class RawKeyboardInputReport : InputReport
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
        ///     The time when the input occurred.
        /// </param>
        /// <param name="actions">
        ///     The set of actions being reported.
        /// </param>
        /// <param name="scanCode">
        ///     The scan code if a key is being reported.
        /// </param>
        /// <param name="isExtendedKey">
        ///     The true if a key is an extended key.
        /// </param>
        /// <param name="isSystemKey">
        ///     The true if a key is a system key.
        /// </param>
        /// <param name="virtualKey">
        ///     The Win32 virtual key code if a key is being reported.
        /// </param>
        /// <param name="extraInformation">
        ///     Any extra information being provided along with the input.
        /// </param>
        public RawKeyboardInputReport(
            PresentationSource inputSource,
            InputMode mode,
            int timestamp, 
            RawKeyboardActions actions, 
            int scanCode, 
            bool isExtendedKey,
            bool isSystemKey,
            int virtualKey, 
            IntPtr extraInformation) : base(inputSource, InputType.Keyboard, mode, timestamp)
        {
            if (!IsValidRawKeyboardActions(actions))
                throw new System.ComponentModel.InvalidEnumArgumentException("actions", (int)actions, typeof(RawKeyboardActions));

            _actions = actions;
            _scanCode = scanCode;
            _isExtendedKey = isExtendedKey;
            _isSystemKey = isSystemKey;
            _virtualKey = virtualKey;
            _extraInformation = new SecurityCriticalData<IntPtr>(extraInformation);
        }

        /// <summary>
        ///     Read-only access to the set of actions that were reported.
        /// </summary>
        public RawKeyboardActions Actions {get {return _actions;}}

        /// <summary>
        ///     Read-only access to the scan code that was reported.
        /// </summary>
        public int ScanCode {get {return _scanCode;}}

        /// <summary>
        ///     Read-only access to the flag of an extended key.
        /// </summary>
        public bool IsExtendedKey {get {return _isExtendedKey;}}

        /// <summary>
        ///     Read-only access to the flag of a system key.
        /// </summary>
        public bool IsSystemKey {get {return _isSystemKey;}}

        /// <summary>
        ///     Read-only access to the virtual key that was reported.
        /// </summary>
        public int VirtualKey {get {return _virtualKey;}}

        /// <summary>
        ///     Read-only access to the extra information was provided along
        ///     with the input.
        /// </summary>
        public IntPtr ExtraInformation
        {
            get
            {
                return _extraInformation.Value;
            }
        }

        // IsValid Method for RawKeyboardActions. Relies on the enum being flags.
        internal static bool IsValidRawKeyboardActions(RawKeyboardActions actions)
        {
            if (((RawKeyboardActions.AttributesChanged | RawKeyboardActions.Activate | RawKeyboardActions.Deactivate |
                  RawKeyboardActions.KeyDown | RawKeyboardActions.KeyUp) & actions) == actions)
            {
                if (!((((RawKeyboardActions.KeyUp | RawKeyboardActions.KeyDown) & actions) == (RawKeyboardActions.KeyUp | RawKeyboardActions.KeyDown)) ||
                      ((RawKeyboardActions.Deactivate & actions) == actions && RawKeyboardActions.Deactivate != actions)))
                {
                    return true;
                }
            }
	    return false;
        }

        private RawKeyboardActions _actions;
        private int _scanCode;
        private bool _isExtendedKey;
        private bool _isSystemKey;
        private int _virtualKey;
        private SecurityCriticalData<IntPtr> _extraInformation;
    }    
}

