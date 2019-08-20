// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;

using System;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Describes the behavior of an animation.
    /// </summary>
    internal enum AnimationType : byte
    {
        /// <summary>
        /// The animation animates from the defaultOriginValue value to the defaultDestinationValue.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// The animation animates from the From property value to the defaultDestinationValue.
        /// </summary>
        From,

        /// <summary>
        /// The animation animates from the defaultOriginValue to the To property value.
        /// </summary>
        To,
        
        /// <summary>
        /// The animation animates from the defaultOriginValue value to the defaultOriginValue value plus
        /// the By property value.
        /// </summary>
        By,

        /// <summary>
        /// The animation animates from the From property value to the To property value.
        /// </summary>
        FromTo,

        /// <summary>
        /// The animation animates from the From property value to the From property value plus
        /// the By property value.
        /// </summary>
        FromBy
    }
}
