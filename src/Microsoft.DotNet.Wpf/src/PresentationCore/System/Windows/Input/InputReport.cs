// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;
using System.Windows;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The InputReport is an abstract base class for all input that is
    ///     reported to the InputManager.
    /// </summary>
    /// <remarks>
    ///     It is important to note that the InputReport class only contains
    ///     blittable types.  This is required so that the report can be
    ///     marshalled across application domains.
    /// </remarks>
    [FriendAccessAllowed]
    internal abstract class InputReport
    {
        /// <summary>
        ///     Constructs ad instance of the InputReport class.
        /// </summary>
        /// <param name="inputSource">
        ///     The type of input that is being reported.
        /// </param>
        /// <param name="type">
        ///     The type of input that is being reported.
        /// </param>
        /// <param name="mode">
        ///     The mode in which the input is being reported.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred.
        /// </param>
        protected InputReport(PresentationSource inputSource, InputType type, InputMode mode, int timestamp)
        {
            if (inputSource == null)
                throw new ArgumentNullException("inputSource");

            Validate_InputType( type );
            Validate_InputMode( mode );
            _inputSource= new SecurityCriticalData<PresentationSource>(inputSource);
            _type = type;
            _mode = mode;
            _timestamp = timestamp;
        }

        /// <summary>
        ///     Read-only access to the type of input source that reported input.
        /// </summary>
        public PresentationSource InputSource 
        { 
            get 
            {
                return _inputSource.Value;
            }
        }

        /// <summary>
        ///     Read-only access to the type of input that was reported.
        /// </summary>
        public InputType Type {get {return _type;}}

        /// <summary>
        ///     Read-only access to the mode in which the input was reported.
        /// </summary>
        public InputMode Mode {get {return _mode;}}

        /// <summary>
        ///     Read-only access to the time when the input occurred.
        /// </summary>
        public int Timestamp {get {return _timestamp;}}

        /// <summary>
        /// There is a proscription against using Enum.IsDefined().  (it is slow)
        /// so we write these PRIVATE validate routines instead.
        /// </summary>
        private void Validate_InputMode( InputMode mode )
        {
            switch( mode )
            {
                case InputMode.Foreground:
                case InputMode.Sink:
                    break;
                default:
                    throw new  System.ComponentModel.InvalidEnumArgumentException("mode", (int)mode, typeof(InputMode));
            }
        }

        /// <summary>
        /// There is a proscription against using Enum.IsDefined().  (it is slow)
        /// so we write these PRIVATE validate routines instead.
        /// </summary>
        private void Validate_InputType( InputType type )
        {
            switch( type )
            {
                case InputType.Keyboard:
                case InputType.Mouse:
                case InputType.Stylus:
                case InputType.Hid:
                case InputType.Text:
                case InputType.Command:
                    break;
                default:
                    throw new  System.ComponentModel.InvalidEnumArgumentException("type", (int)type, typeof(InputType));
            }
        }

        private SecurityCriticalData<PresentationSource> _inputSource;
        private InputType _type;
        private InputMode _mode;
        private int _timestamp;
    }
}
