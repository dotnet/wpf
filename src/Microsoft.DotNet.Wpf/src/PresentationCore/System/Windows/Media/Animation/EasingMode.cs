// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This enum defines the modes in which classes deriving from EasingFunctioBase
    ///     can will perform their easing.
    /// </summary>
    public enum EasingMode
    {
        EaseIn,    // the easing is performed at the start of the animation
        EaseOut,   // the easing is performed at the end of the animation
        EaseInOut, // the easing is performed both at the start and the end of the animation
    }
}
