// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implements services for Drawings including walking, bounding,
//              and hit-testing.
//

using System.Diagnostics;
using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    /// <summary>
    /// Implements services for Drawings including walking, bounding, and
    /// hit-testing.
    /// </summary>
    internal static class DrawingServices
    {
        /// <summary>
        /// Determines whether or not a point exists in a Drawing
        /// </summary>
        /// <param name="drawing"> Drawing to hit-test</param>
        /// <param name="point"> Point to hit-test for </param>
        /// <returns>
        /// 'true' if the point exists within the drawing, 'false' otherwise
        /// </returns>
        internal static bool HitTestPoint(Drawing drawing, Point point)
        {
            if (drawing != null)
            {
                HitTestDrawingContextWalker ctx = new HitTestWithPointDrawingContextWalker(point);

                drawing.WalkCurrentValue(ctx);

                return ctx.IsHit;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hit-tests a Drawing against a PathGeometry
        /// </summary>
        /// <param name="drawing"> The drawing to hit test against </param>
        /// <param name="geometry"> The geometry (in local coordinate space) to hit test. </param>
        /// <returns>
        /// IntersectionDetail that describes the hit result
        /// </returns>
        internal static IntersectionDetail HitTestGeometry(Drawing drawing, PathGeometry geometry)
        {
            if (drawing != null)
            {
                HitTestDrawingContextWalker ctx =
                    new HitTestWithGeometryDrawingContextWalker(geometry);

                drawing.WalkCurrentValue(ctx);

                return ctx.IntersectionDetail;
            }
            else
            {
                return IntersectionDetail.Empty;
            }
        }

        /// <summary>
        /// Converts a RenderData content representation into a DrawingGroup
        /// content representation.
        /// </summary>
        /// <param name="renderData"> The RenderData to convert </param>
        /// <returns>
        /// A new DrawingGroup representation that is functionally equivalent to the
        /// passed-in RenderData.
        /// </returns>
        internal static DrawingGroup DrawingGroupFromRenderData(RenderData renderData)
        {
            //
            // Create & open a new DrawingGroup
            //

            DrawingGroup drawingGroup = new DrawingGroup();

            DrawingContext dc = drawingGroup.Open();

            //
            // Create a DrawingGroup from the RenderData by walking
            // the RenderData & having it forward it's base value's
            // and animations to DrawingGroup
            //

            //
            // The Drawing tree we're about to produce should not be an inheritance context,
            // since that would place all mutable Freezables in the render data into shared
            // state, which would in turn case them to lose their inheritance context entirely.
            // This is controlled by setting "CanBeInheritanceContext" to false on the
            // DrawingContext which will then be applied to all new objects it creates.
            //

            DrawingDrawingContext ddc = dc as DrawingDrawingContext;

            if (ddc != null)
            {
                ddc.CanBeInheritanceContext = false;
            }

            DrawingContextDrawingContextWalker walker =
                new DrawingContextDrawingContextWalker(dc);

            renderData.BaseValueDrawingContextWalk(walker);

            //
            // Close the DrawingContext & return the new DrawingGroup
            //

            dc.Close();

            return drawingGroup;
        }
    }
}

