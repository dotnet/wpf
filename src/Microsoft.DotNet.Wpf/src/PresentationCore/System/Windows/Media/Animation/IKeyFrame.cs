// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This interface should be implemented by all key frames to provide
    /// untyped access to the key time and value.
    /// </summary>
    public interface IKeyFrame
    {
        /// <summary>
        /// The key time associated with the key frame.
        /// </summary>
        /// <value></value>
        KeyTime KeyTime { get; set; }

        /// <summary>
        /// The value associated with the key frame.
        /// </summary>
        /// <value></value>
        object Value { get; set; }
    }
}

