// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: GlyphRunDrawing represents a drawing operation that renders 
//              a GlyphRun.
//

using System.Diagnostics;

namespace System.Windows.Media
{
    /// <summary>
    /// GlyphRunDrawing represents a drawing operation that renders a GlyphRun.
    /// </summary>
    public sealed partial class GlyphRunDrawing : Drawing
    {
        #region Constructors

        /// <summary>
        /// Default GlyphRunDrawing constructor.  
        /// Constructs an object with all properties set to their default values
        /// </summary>        
        public GlyphRunDrawing()
        {
        }

        /// <summary>
        /// Two-argument GlyphRunDrawing constructor.
        /// Constructs an object with the GlyphRun and ForegroundBrush properties
        /// set to the value of their respective arguments.
        /// </summary>        
        public GlyphRunDrawing(Brush foregroundBrush, GlyphRun glyphRun)
        {            
            GlyphRun = glyphRun;
            ForegroundBrush = foregroundBrush;
        }               

        #endregion       

        #region Internal methods

        /// <summary>
        /// Calls methods on the DrawingContext that are equivalent to the
        /// Drawing with the Drawing's current value.
        /// </summary>        
        internal override void WalkCurrentValue(DrawingContextWalker ctx)
        {
            // We avoid unneccessary ShouldStopWalking checks based on assumptions
            // about when ShouldStopWalking is set.  Guard that assumption with an
            // assertion.  See DrawingGroup.WalkCurrentValue comment for more details.
            Debug.Assert(!ctx.ShouldStopWalking);
            
            ctx.DrawGlyphRun(
                ForegroundBrush,
                GlyphRun
                );                              
        }

        #endregion Internal methods
    }
}

