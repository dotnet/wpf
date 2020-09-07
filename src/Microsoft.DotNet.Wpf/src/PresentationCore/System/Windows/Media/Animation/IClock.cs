// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using MS.Internal;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Represents an object that can provide linear time values as a TimeSpan. 
    /// Note that this interface is internal.
    /// </summary>
    /// <remarks>
    /// The IClock interface must be implemented by any objects that wish to act as the
    /// clock driving the timing engine. This interface contains a single property that
    /// the time manager calls periodically to obtain the current time.
    /// </remarks>
    /// <example>
    /// The following example shows how to replace the clock used by a time manager:
    /// <code>
    /// class CustomClock : IClock
    /// {
    ///     public TimeSpan CurrentTime
    ///     {
    ///         get
    ///         {
    ///             // This example uses the system's tick count, but anything else would
    ///             // be valid. Note that the time manager expects non-decreasing time
    ///             // values
    ///             return TimeSpan.FromTicks(System.Environment.TickCount);
    ///         }
    ///     }
    /// }
    /// 
    /// class MyApp
    /// {
    ///     public void ReplaceTimingEngineClock(TimeManager manager)
    ///     {
    ///         CustomClock myClock = new CustomClock;
    ///         
    ///         manager.Clock = (IClock)myClock;
    ///     }
    /// }
    /// </code>
    /// </example>
    internal interface IClock
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        TimeSpan CurrentTime
        {
            get;
        }
    }
}
