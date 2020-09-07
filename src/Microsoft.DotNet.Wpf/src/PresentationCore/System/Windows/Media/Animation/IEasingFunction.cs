// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This interface represents a transformation of normalizedTime.  Animations use it to 
    ///     transform their progress before computing an interpolation.  Classes that implement
    ///     this interface can control the pace at which an animation is performed.
    /// </summary>
    public interface IEasingFunction
    {
        /// <summary>
        ///     Transforms normalized time to control the pace of an animation.
        /// </summary>
        /// <param name="normalizedTime">normalized time (progress) of the animation</param>
        /// <returns>transformed progress</returns>
        double Ease(double normalizedTime);
    }
}
