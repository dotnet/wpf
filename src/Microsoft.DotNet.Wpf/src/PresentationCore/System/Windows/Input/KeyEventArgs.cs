// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Security; 
using MS.Internal; 
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Input 
{
    /// <summary>
    ///     The KeyEventArgs class contains information about key states.
    /// </summary>
    /// <ExternalAPI/>     
    public class KeyEventArgs : KeyboardEventArgs
    {
        /// <summary>
        ///     Constructs an instance of the KeyEventArgs class.
        /// </summary>
        /// <param name="keyboard">
        ///     The logical keyboard device associated with this event.
        /// </param>
        /// <param name="inputSource">
        ///     The input source that provided this input.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="key">
        ///     The key referenced by the event.
        /// </param>
        public KeyEventArgs(KeyboardDevice keyboard, PresentationSource inputSource, int timestamp, Key key) : base(keyboard, timestamp)
        {
            if (inputSource == null)
                throw new ArgumentNullException("inputSource");

            if (!Keyboard.IsValidKey(key))
                throw new System.ComponentModel.InvalidEnumArgumentException("key", (int)key, typeof(Key));

            _inputSource = inputSource;

            _realKey = key;
            _isRepeat = false;

            // Start out assuming that this is just a normal key.
            MarkNormal();
        }

        /// <summary>
        ///     The input source that provided this input.
        /// </summary>
        public PresentationSource InputSource
        {
            get 
            {
                
                return UnsafeInputSource;
            }
        }

        /// <summary>
        ///     The Key referenced by the event, if the key is not being 
        ///     handled specially.
        /// </summary>
        public Key Key
        {
            get {return _key;}
        }

        /// <summary>
        ///     The original key, as opposed to <see cref="Key"/>, which might
        ///     have been changed (e.g. by MarkTextInput).
        /// </summary>
        /// <remarks>
        /// Note:  This should remain internal.  When a processor obfuscates the key,
        /// such as changing characters to Key.TextInput, we're deliberately trying to
        /// hide it and make it hard to find.  But internally we'd like an easy way to find
        /// it.  So we have this internal, but it must remain internal.
        /// </remarks>
        internal Key RealKey
        {
            get { return _realKey; }
        }

        /// <summary>
        ///     The Key referenced by the event, if the key is going to be
        ///     processed by an IME.
        /// </summary>
        public Key ImeProcessedKey
        {
            get { return (_key == Key.ImeProcessed) ? _realKey : Key.None;}
        }

        /// <summary>
        ///     The Key referenced by the event, if the key is going to be
        ///     processed by an system.
        /// </summary>
        public Key SystemKey
        {
            get { return (_key == Key.System) ? _realKey : Key.None;}
        }

        /// <summary>
        ///     The Key referenced by the event, if the the key is going to 
        ///     be processed by Win32 Dead Char System.
        /// </summary>
        public Key DeadCharProcessedKey
        {
            get { return (_key == Key.DeadCharProcessed) ? _realKey : Key.None; }
        }

        /// <summary>
        ///     The state of the key referenced by the event.
        /// </summary>
        public KeyStates KeyStates
        {
            get {return this.KeyboardDevice.GetKeyStates(_realKey);}
        }

        /// <summary>
        ///     Whether the key pressed is a repeated key or not.
        /// </summary>
        public bool IsRepeat
        {
            get {return _isRepeat;}
        }

        /// <summary>
        ///     Whether or not the key referenced by the event is down.
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public bool IsDown
        {
            get {return this.KeyboardDevice.IsKeyDown(_realKey);}
        }

        /// <summary>
        ///     Whether or not the key referenced by the event is up.
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public bool IsUp
        {
            get {return this.KeyboardDevice.IsKeyUp(_realKey);}
        }

        /// <summary>
        ///     Whether or not the key referenced by the event is toggled.
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public bool IsToggled
        {
            get {return this.KeyboardDevice.IsKeyToggled(_realKey);}
        }

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        /// <ExternalAPI/> 
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            KeyEventHandler handler = (KeyEventHandler) genericHandler;
            
            handler(genericTarget, this);
        }

        internal void SetRepeat( bool newRepeatState )
        {
            _isRepeat = newRepeatState;
        }

        internal void MarkNormal()
        {
            _key = _realKey;
        }

        internal void MarkSystem()
        {
            _key = Key.System;
        }
        
        internal void MarkImeProcessed()
        {
            _key = Key.ImeProcessed;
        }

        internal void MarkDeadCharProcessed()
        {
            _key = Key.DeadCharProcessed;
        }
        internal PresentationSource UnsafeInputSource
        {
            get 
            {
                return _inputSource;
            }
        }

        internal int ScanCode
        {
            get {return _scanCode;}
            set {_scanCode = value;}
        }

        internal bool IsExtendedKey
        {
            get {return _isExtendedKey;}
            set {_isExtendedKey = value;}
        }


        private Key _realKey;
        private Key _key;

        private PresentationSource _inputSource;

        private bool _isRepeat;
        private int _scanCode;
        private bool _isExtendedKey;
}
}

