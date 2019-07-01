// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

namespace Microsoft.Test.Input
{
    /// <summary>
    ///     The RawKeyboardState class encapsulates the state of the keyboard.
    /// </summary>
    public class RawKeyboardState
    {
        /// <summary>
        ///     Constructs an instance of the RawKeyboardState class.
        /// </summary>
        /// <param name="virtualKeys">
        ///     The state of each of the virtual keys.
        /// </param>
        public RawKeyboardState(KeyStates[] virtualKeys)
        {
            if (virtualKeys == null || virtualKeys.Length == 0)
                throw new ArgumentNullException("virtualKeys");

            _virtualKeys = virtualKeys;
        }

        /// <summary>
        ///     Read-only access to the state of each of the virtual keys.
        /// </summary>
        public KeyStates[] VirtualKeys {get {return _virtualKeys;}}

        private KeyStates[] _virtualKeys;
    }    
}


