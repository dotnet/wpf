// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This enumeration represents the different states that a clock can be in
    /// at any given time.
    /// </summary>
    public enum ClockState
    {
        /// <summary>
        /// The clock is currently active meaning that the current time of the
        /// clock changes relative to its parent time. 
        /// </summary>
        Active,

        /// <summary>
        /// The clock is currenty in a fill state meaning that its current time
        /// and progress do not change relative to the parent's time, but the 
        /// clock is not stopped.
        /// </summary>
        Filling,

        /// <summary>
        /// The clock is currently stopped which means that its current time and
        /// current progress property values are undefined. 
        /// </summary>
        Stopped
    }
}
