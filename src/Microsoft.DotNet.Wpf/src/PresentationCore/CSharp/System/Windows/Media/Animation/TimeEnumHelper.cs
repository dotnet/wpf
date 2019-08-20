// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using System.Windows.Media.Animation;

namespace MS.Internal
{
    /// <summary>
    /// A class for validating enumerated types.
    /// </summary>
    internal static partial class TimeEnumHelper
    {
        // IMPORTANT: These values must be kept current with enum definitions for validation to work

        // Enums declared in Enums.cs
        private const int _maxTimeSeekOrigin = (int)TimeSeekOrigin.Duration;

        // Enums declared in PathAnimationSource.cs
        private const byte _maxPathAnimationSource = (int)PathAnimationSource.Angle;

        /// <summary>
        /// Determines if the enumerated value is defined (valid) for the given enumerated type
        /// </summary>
        /// <param name="value">
        /// The variable whose validity is verified.
        /// </param>
        /// <returns>
        /// True if valid, false otherwise.
        /// </returns>
        static internal bool IsValidTimeSeekOrigin(TimeSeekOrigin value)
        {
            return (0 <= value && (int)value <= _maxTimeSeekOrigin);
        }

        /// <summary>
        /// Determines if the enumerated value is defined (valid) for the given enumerated type
        /// </summary>
        /// <param name="value">
        /// The variable whose validity is verified.
        /// </param>
        /// <returns>
        /// True if valid, false otherwise.
        /// </returns>
        static internal bool IsValidPathAnimationSource(PathAnimationSource value)
        {
            return (0 <= value && (byte)value <= _maxPathAnimationSource);
        }
    }
}
