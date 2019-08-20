// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of a GradientStop.
//              The GradientStop class contains a color and an offset, and is
//              used by GradientBrush.
//
//

using MS.Internal;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;

namespace System.Windows.Media 
{
    /// <summary>
    /// GradientStop - The gradient stop is a tuple consisting of an offset and a color.
    /// A collection of gradient stops help describe the contents of a GradientBrush.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed partial class GradientStop : Animatable
    {
        #region Constructors
        
        /// <summary>
        /// GradientStop - Initialize this GradientStop
        /// </summary>
        public GradientStop()
        {
        }

        /// <summary>
        /// GradientStop - Initialize this GradientStop from a constant Color and offset.
        /// </summary>
        /// <param name="color"> The Color at this offset. </param>
        /// <param name="offset"> The offset within the Gradient. </param>
        public GradientStop(Color color, double offset)
        {
            Color = color;
            Offset = offset;
        }

        #endregion Constructors
    }
}
