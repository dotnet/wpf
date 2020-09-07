// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;

namespace System.Windows.Input
{
    /// <summary>
    ///     Configuration options for manipulation.
    /// </summary>
    [Flags]
    public enum ManipulationModes
    {
        /// <summary>
        ///     No manipulation recognition.
        /// </summary>
        None                                = 0x0000,

        /// <summary>
        ///     Recognizes changes to location along X axis.
        /// </summary>
        TranslateX                          = 0x0001,

        /// <summary>
        ///     Recognizes changes to location along Y axis.
        /// </summary>
        TranslateY                          = 0x0002,

        /// <summary>
        ///     Recognizes changes to location.
        /// </summary>
        Translate                           = TranslateX | TranslateY,

        /// <summary>
        ///     Recognizes changes to orientation.
        /// </summary>
        Rotate                              = 0x0004,

        /// <summary>
        ///     Recognizes changes to size.
        /// </summary>
        Scale                               = 0x0008,

        /// <summary>
        ///     All manipulations recognized.
        ///     Only touch input is recognized for single input recognition.
        /// </summary>
        All                                 = Translate | Rotate | Scale,
    }
}
