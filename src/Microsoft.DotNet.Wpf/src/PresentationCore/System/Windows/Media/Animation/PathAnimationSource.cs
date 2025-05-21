// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Media.Animation
{
    // IMPORTANT: If you change public Enums, TimeEnumHelper.cs must be updated
    //  to reflect the maximum enumerated values for the public enums.

    /// <summary>
    /// Enumeration of the possible output values of a path that may be
    /// used by a path animation.
    /// </summary>
    public enum PathAnimationSource : byte
    {
        /// <summary>
        /// Represent the X offset of the progress along the path.
        /// </summary>
        X                   = 0,

        /// <summary>
        /// Represent the Y offset of the progress along the path.
        /// </summary>
        Y                   = 1,

        /// <summary>
        /// Represents the tangent angle of rotation of the progress along the path.
        /// </summary>
        Angle               = 2,
    }
}
