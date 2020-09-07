// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using MS.Internal;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Animation
{
    // IMPORTANT: If you change public Enums, TimeEnumHelper.cs must be updated
    //  to reflect the maximum enumerated values for the public enums.

    /// <summary>
    /// Specifies the behavior of the <see cref="ClockController.Seek"/>
    /// method by defining the meaning of that method's offset parameter.
    /// </summary>
    public enum TimeSeekOrigin
    {
        /// <summary>
        /// The offset parameter specifies the new position of the timeline as a
        /// distance from time t=0
        /// </summary>
        BeginTime,

        /// <summary>
        /// The offset parameter specifies the new position of the timeline as a
        /// distance from the end of the simple duration. If the duration is not
        /// defined, this causes the method call to have no effect.
        /// </summary>
        Duration
    }

    /// <summary>
    /// Possible states the time manager may be in.
    /// </summary>
    /// <remarks>
    /// this structure is obsolete
    /// </remarks>
    internal enum TimeState
    {
        /// <summary>
        /// Time is stopped.
        /// </summary>
        /// <remarks>
        /// This is the default (constructor) value.
        /// </remarks>
        Stopped,
        /// <summary>
        /// Time is paused.
        /// </summary>
        Paused,
        /// <summary>
        /// Time is running.
        /// </summary>
        Running,
    }
}
