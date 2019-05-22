// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of DrawingBrush.
//              The DrawingBrush is a TileBrush which defines its tile content
//              by use of a Drawing.


using MS.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media 
{
    /// <summary>
    /// DrawingBrush - This TileBrush defines its content as a Drawing
    /// </summary>
    public sealed partial class DrawingBrush : TileBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for DrawingBrush.  The resulting Brush has no content.
        /// </summary>
        public DrawingBrush()
        {
        }

        /// <summary>
        /// DrawingBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="drawing"> The Drawing representing the contents of this Brush. </param>
        public DrawingBrush(Drawing drawing)
        {
            Drawing = drawing;
        }

        #endregion Constructors

        /// <summary>
        /// Obtains the current bounds of the brush's content
        /// </summary>
        /// <param name="contentBounds"> Output bounds of content </param>  
        protected override void GetContentBounds(out Rect contentBounds)
        {
            contentBounds = Drawing.GetBounds();
        }
}
}
