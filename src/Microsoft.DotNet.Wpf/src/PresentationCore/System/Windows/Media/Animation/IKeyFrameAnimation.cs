// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This interface should be implemented by all key frame animations to
    /// provide untyped access to the key frame collection.
    /// </summary>
    public interface IKeyFrameAnimation
    {
        /// <summary>
        /// The key frame collection associated with the key frame animation.
        /// </summary>
        /// <value></value>
        IList KeyFrames { get; set; }
    }
}

