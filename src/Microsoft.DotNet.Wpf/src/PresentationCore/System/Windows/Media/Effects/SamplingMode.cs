// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft Windows Presentation Foundation
//

namespace System.Windows.Media.Effects
{
    public enum SamplingMode
    {
        // These values need to match how they're interpeted on the native
        // side.
        
        /// <summary>
        /// Always use nearest neighbor sampling.
        /// </summary>
        NearestNeighbor = 0x0,

        /// <summary>
        /// Always use bilinear sampling.
        /// </summary>
        Bilinear        = 0x1,

        /// <summary>
        /// System automatically chooses most appropriate sampling mode.
        /// </summary>
        Auto            = 0x2,
    }
}
