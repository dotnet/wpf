// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The mode of input processing when the input was provided.
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        ///     Input was provided while the application was in the
        ///     foreground.
        /// </summary>
        Foreground,

        /// <summary>
        ///     Input was provided while the application was not in the
        ///     foreground.
        /// </summary>
        Sink
    }
}

