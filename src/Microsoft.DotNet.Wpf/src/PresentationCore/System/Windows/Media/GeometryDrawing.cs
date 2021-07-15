// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: GeometryDrawing represents a drawing operation that combines 
//              a geometry with and brush and/or pen to produce rendered
//              content.
//

using System.Diagnostics;

namespace System.Windows.Media
{
    /// <summary>
    /// GeometryDrawing represents a drawing operation that combines 
    /// a geometry with and brush and/or pen to produce rendered
    /// content.
    /// </summary>    
    public sealed partial class GeometryDrawing : Drawing
    {
        #region Constructors

        /// <summary>
        /// Default GeometryDrawing constructor.  
        /// Constructs an object with all properties set to their default values
        /// </summary>        
        public GeometryDrawing()
        {
        }

        /// <summary>
        /// Three-argument GeometryDrawing constructor.
        /// Constructs an object with the Brush, Pen, and Geometry properties
        /// set to the value of their respective arguments.
        /// </summary>        
        public GeometryDrawing(Brush brush, Pen pen, Geometry geometry)
        {
            Brush = brush;
            Pen = pen;
            Geometry = geometry;
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
            
            ctx.DrawGeometry(
                Brush,
                Pen,
                Geometry
                );            
        }

        #endregion Internal methods
    }
}

