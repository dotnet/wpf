// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Ink
{
    /// <summary>
    /// StylusTip
    /// </summary>
    public enum StylusTip
    {
        /// <summary>
        /// Rectangle
        /// </summary>
        Rectangle = 0,

        /// <summary>
        /// Ellipse
        /// </summary>
        Ellipse
    }

    /// <summary>
    /// Internal helper to avoid costly call to Enum.IsDefined
    /// </summary>
    internal static class StylusTipHelper
    {
        internal static bool IsDefined(StylusTip stylusTip)
        {
            if (stylusTip < StylusTip.Rectangle || stylusTip > StylusTip.Ellipse)
            {
                return false;
            }
            return true;
        }
    }
}
