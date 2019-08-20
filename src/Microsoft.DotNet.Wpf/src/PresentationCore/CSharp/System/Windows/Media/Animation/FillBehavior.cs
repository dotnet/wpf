// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System.Windows.Media.Animation;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// The FillBehavior enumeration is used to indicate how a Timeline will behave
    /// when it is outside of its active period but its parent is inside its 
    /// active period.
    /// </summary>
    public enum FillBehavior
    {
        /// <summary>
        /// Indicates that a Timeline will hold its progress between the period of
        /// time between the end of its active period and the end of its parents active and
        /// hold periods. 
        /// </summary>
        HoldEnd,

#if IMPLEMENTED // Uncomment when implemented

        /// <summary>
        /// Indicates that a Timeline will hold its initial active progress during the
        /// period of time between when its parent has become active and it 
        /// becomes active. The Timeline will stop after the completion of
        /// its own active period.
        /// </summary>
        HoldBegin,

        /// <summary>
        /// Indicates that a Timeline will hold its progress both before and after
        /// its active period as long as its parent is in its active or hold periods.
        /// </summary>
        HoldBeginAndEnd

#endif

        /// <summary>
        /// Indicates that a Timeline will stop if it's outside its active
        /// period while its parent is inside its active period.
        /// </summary>
        Stop,
    }
}

namespace MS.Internal
{
    internal static partial class TimeEnumHelper
    {
        private const int c_maxFillBehavior = (int)FillBehavior.Stop;

        static internal bool IsValidFillBehavior(FillBehavior value)
        {
            return (0 <= value && (int)value <= c_maxFillBehavior);
        }
    }
}
