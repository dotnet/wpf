// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///		The state of the StylusDevice hardware button.
    /// </summary>
    /// <ExternalAPI/>
    public enum StylusButtonState
    {
        /// <summary>
        ///  The StylusDevice button is not pressed, and is in the up position.
        /// </summary>
        Up = 0,
        /// <summary>
        ///  The StylusDevice button is pressed, and is in the down position.
        /// </summary>
        Down = 1,
    }
}
