// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Media.Animation
{
    /// <summary>
    /// The different types of KeyTimes
    /// </summary>
    public enum KeyTimeType : byte
    {
        /// <summary>
        /// Adjacent KeyFrames with Uniform KeyTimes will each be given an equal
        /// portion of the available time.
        /// </summary>
        Uniform = 0,

        /// <summary>
        /// KeyTime is a percent of the duration of the animation.
        /// </summary>
        Percent,

        /// <summary>
        /// KeyTime is a TimeSpan relative to the BeginTime of the animation.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// Adjacent KeyFrames with Paced KeyTimes will each be given a portion
        /// of the available time proportional to their length.  For some types,
        /// such as <see cref="System.Windows.Rect">Rect</see> a length value
        /// may be difficult to visualize, but the goal is to produce a length
        /// value that will keep the pace of the animation constant.
        /// </summary>
        Paced
    }
}
