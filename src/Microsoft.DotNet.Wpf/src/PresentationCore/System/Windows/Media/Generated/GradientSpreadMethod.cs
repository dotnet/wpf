// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//


namespace System.Windows.Media
{
    /// <summary>
    ///     GradientSpreadMethod - This determines how a gradient fills the space outside its 
    ///     primary area.
    /// </summary>
    public enum GradientSpreadMethod
    {
        /// <summary>
        ///     Pad - Pad - The final color in the gradient is used to fill the remaining area.
        /// </summary>
        Pad = 0,

        /// <summary>
        ///     Reflect - Reflect - The gradient is mirrored and repeated, then mirrored again, 
        ///     etc.
        /// </summary>
        Reflect = 1,

        /// <summary>
        ///     Repeat - Repeat - The gradient is drawn again and again.
        /// </summary>
        Repeat = 2,
    }   
}
