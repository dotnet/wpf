// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using Microsoft.Internal.AlphaFlattener;

using MS.Internal;

namespace System.Windows.Xps.Serialization
{
    #region internal class DrawingContextFlattener
    /// <summary>
    /// DrawingContext flattening filter
    /// 1) Convert animation to static value
    /// 2) Rasterize video/3D to bitmap
    /// 3) Convert shapes to Geometry
    /// 4) Convert VisualBrush to DrawingBrush
    /// 5) Convert DrawingImage to ImageBrush
    /// 6) Flatten DrawDrawing
    /// </summary>
    internal class DrawingContextFlattener
    {
        #region Constants

        /// <summary>
        /// Percentage to inflate rasterization clip rectangle to prevent prematurely clipping
        /// Visuals at page edges.
        /// </summary>
        private const double RasterizationClipInflate = 0.2;

        #endregion
        
        #region Private Fields

        private IMetroDrawingContext _dc;

        // Stores pushed transforms. Each transform contains all prior transforms.
        private List<Matrix> _fullTransform = new List<Matrix>();

        // Pushed combined clipping in world space.
        private List<Geometry> _fullClip = new List<Geometry>();

        private Size     _pageSize;
        
        // Used to track visual brushes whos visuals are being traversed. We do this to detect cycles in the visual tree
        TreeWalkProgress _treeWalkProgress;
        
        #endregion

        #region Constructors

        internal DrawingContextFlattener(IMetroDrawingContext dc, Size pageSize, TreeWalkProgress treeWalkProgress)
        {
            _dc       = dc;
            _pageSize = pageSize;
            _treeWalkProgress = treeWalkProgress;
        }

        #endregion

        #region Public State

        public void Push(
            Transform transform,
            Geometry clip,
            double opacity,
            Brush opacityMask,
            Rect maskBounds,
            bool onePrimitive,
            String nameAttr,
            Visual node,
            Uri navigateUri,
            EdgeMode edgeMode)
        {
            Debug.Assert(Utility.IsValid(opacity), "Invalid opacity should clip subtree");

            Matrix mat = Matrix.Identity;

            if (transform != null)
            {
                mat = transform.Value;
            }

            // opacity mask might be VisualBrush, hence ReduceBrush to reduce to DrawingBrush
            Debug.Assert(!BrushProxy.IsEmpty(opacityMask), "empty opacity mask should not result in Push");
            _dc.Push(
                mat,
                clip,
                opacity,
                ReduceBrush(opacityMask, maskBounds),
                maskBounds,
                onePrimitive,
                nameAttr,
                node,
                navigateUri,
                edgeMode
                );

            // prepend to transforms and clipping stack
            mat.Append(Transform);
            _fullTransform.Add(mat);

            // transform clip to world space, intersect with current clip, and push
            if (clip == null)
            {
                // push current clipping
                clip = Clip;
            }
            else
            {
                clip = Utility.TransformGeometry(clip, Transform);

                bool empty;
                clip = Utility.Intersect(clip, Clip, Matrix.Identity, out empty);

                if (empty)
                {
                    clip = Geometry.Empty;
                }
            }

            _fullClip.Add(clip);
        }

        /// <summary>
        /// Pop the most recent Push operation
        /// </summary>
        public void Pop()
        {
            _dc.Pop();

            Debug.Assert(_fullTransform.Count == _fullClip.Count);

            int lastIndex = _fullTransform.Count - 1;

            _fullTransform.RemoveAt(lastIndex);
            _fullClip.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Transformation representing all pushed transformations.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                if (_fullTransform.Count == 0)
                {
                    return Matrix.Identity;
                }
                else
                {
                    return _fullTransform[_fullTransform.Count - 1];
                }
            }
        }

        /// <summary>
        /// Intersection of all pushed clipping in world space.
        /// </summary>
        public Geometry Clip
        {
            get
            {
                if (_fullClip.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _fullClip[_fullClip.Count - 1];
                }
            }
        }

        #endregion

        #region Public Methods

#if DEBUG
        /// <summary>
        /// Add comment to output, as debugging aid or add extra information like document structure
        /// </summary>
        /// <param name="str"></param>
        public void Comment(String str)
        {
            _dc.Comment(str);
        }
#endif

        /// <summary>
        /// Simplifies brush so we don't have to handle as many cases.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds">Brush fill bounds; must not be empty if VisualBrush</param>
        /// <returns></returns>
        /// <remarks>
        /// Cases simplified:
        /// - A lot of empty brush cases. See BrushProxy.IsEmpty.
        /// - GradientBrush where gradient colors are similar enough to be SolidColorBrush.
        /// - Reduce VisualBrush to DrawingBrush.
        /// - Reduce ImageBrush with DrawingImage to DrawingBrush.
        /// </remarks>
        private Brush ReduceBrush(Brush brush, Rect bounds)
        {
            return BrushProxy.ReduceBrush(brush, bounds, Transform, _pageSize, _treeWalkProgress);            
        }

        private Pen ReducePen(Pen pen, Rect bounds)
        {
            if (PenProxy.IsNull(pen))
            {
                return null;
            }

            Brush b = ReduceBrush(pen.Brush, bounds);

            if (b == null)
            {
                return null;
            }

            if (! Object.ReferenceEquals(b, pen.Brush))
            {
                pen = pen.CloneCurrentValue();

                pen.Brush = b;
            }

            return pen;
        }
        
        /// <summary>
        /// Draw a Geometry with the provided Brush and/or Pen.
        /// </summary>
        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            if (geometry != null)
            {
                if (brush != null)
                {
                    Rect bounds = geometry.Bounds;

                    brush = ReduceBrush(brush, bounds);
                }
                
                if ((pen != null) && (pen.Brush != null))
                {
                    Rect bounds = Rect.Empty;

                    if (VisualSerializer.NeedBounds(pen.Brush))
                    {
                        bounds = geometry.GetRenderBounds(pen);
                    }
                                            
                    pen = ReducePen(pen, bounds);
                }                    
                else
                {
                    pen = null;
                }
                                    
                // Draw even if brush/pen is null since geometry may be a hyperlink.
                // _dc should cull invisible geometries when necessary.
                _dc.DrawGeometry(brush, pen, geometry);
            }
        }

        /// <summary>
        /// Draw an Image into the region specified by the Rect.
        /// </summary>
        public void DrawImage(ImageSource image, Rect rectangle)
        {
            if (image != null)
            {
                DrawingImage drawingImage;
                D3DImage d3dimage;
                
                if (image is BitmapSource)
                {
                    // Apparently, IMetroDrawingContext.DrawImage only handles BitmapSources...
                    _dc.DrawImage(image, rectangle);
                }
                else if ((drawingImage = image as DrawingImage) != null)
                {
                    DrawGeometry(new DrawingBrush(drawingImage.Drawing), null, new RectangleGeometry(rectangle));
                }
                else if ((d3dimage = image as D3DImage) != null)
                {
                    _dc.DrawImage(d3dimage.CopyBackBuffer(), rectangle);
                }
                else
                {
                    Invariant.Assert(false, "Unhandled ImageSource type!");
                }
            }
            else
            {
                // no image, but may still be a hyperlink
                _dc.DrawImage(null, rectangle);
            }
        }

        /// <summary>
        /// Clip visual bounds to rasterization clip rectangle.
        /// </summary>
        /// <param name="visualBounds"></param>
        /// <param name="visualToWorldTransform"></param>
        /// <returns></returns>
        private Rect PerformRasterizationClip(Rect visualBounds, Matrix visualToWorldTransform)
        {
            if (! _pageSize.IsEmpty)
            {
                Rect pageBox = new Rect(0, 0, _pageSize.Width, _pageSize.Height);
                
                pageBox.Inflate(
                    RasterizationClipInflate * pageBox.Width,
                    RasterizationClipInflate * pageBox.Height
                    );

                visualBounds.Intersect(pageBox);
            }
            
            return visualBounds;
        }

        /// <summary>
        /// Rasterizes Visual and its descendents with optional bitmap effect. Also handles Visual opacity.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="nameAttr">Preserve Visual name attribute</param>
        /// <param name="navigateUri">Preserve FixedPage.NavigateUri</param>
        /// <param name="edgeMode">Preserve RenderOptions.EdgeMode</param>
        /// <param name="visualTransform">Visual.Transform</param>
        /// <param name="visualToWorldTransform">Full transform from Visual to world space, including visual transform prepended</param>
        /// <param name="inheritedTransformHint">
        /// Transformation above VisualTreeFlattener instance. This is needed if we're reducing VisualBrush to
        /// DrawingBrush to increase rasterization fidelity if brush is small, but will eventually fill large region.
        /// </param>
        /// <param name="clip">Clip in world space</param>
        /// <param name="effect">Optional bitmap effect</param>
        public void DrawRasterizedVisual(
            Visual visual,
            string nameAttr,
            Uri navigateUri,
            EdgeMode edgeMode,
            Transform visualTransform,
            Matrix visualToWorldTransform,
            Matrix inheritedTransformHint,
            Geometry clip,
            Effect effect
            )
        {
            Debug.Assert(visual != null);

            // Compute the bounding box of the visual and its descendants
            Rect bounds = VisualTreeHelper.GetContentBounds(visual);
            bounds.Union(VisualTreeHelper.GetDescendantBounds(visual));

            if (!Utility.IsRenderVisible(bounds))
                return;

            // transform clip to visual space
            if (clip != null)
            {
                Matrix worldToVisualTransform = visualToWorldTransform;
                worldToVisualTransform.Invert();

                clip = Utility.TransformGeometry(clip, worldToVisualTransform);
            }

            //
            // Clip visual bounds to rasterization clipping geometry.
            // We can't clip to Visual clipping geometry, since bitmap effects are applied
            // without clipping. For example, the blur effect looks different if you clip first
            // then apply effect, compared to applying the effect and then clipping.
            //
            bounds = PerformRasterizationClip(bounds, visualToWorldTransform);

            if (!Utility.IsRenderVisible(bounds))
                return;

            //
            // Rasterize Visual to IMetroDrawingContext with optional banding, depending
            // on whether bitmap effect is present. We can Push/Pop/Draw directly on _dc
            // since the input we provide it is already normalized (no DrawingImage, for example).
            // We also don't need Transform or Clip to be updated.
            //
            _dc.Push(
                visualTransform == null ? Matrix.Identity : visualTransform.Value,
                clip,
                1.0,
                null,
                Rect.Empty,
                /*onePrimitive=*/false, // we Push and DrawImage below, which counts as 2 primitives
                nameAttr,
                visual,
                navigateUri,
                edgeMode
                );

            Matrix bitmapToVisualTransform;
            BitmapSource bitmap;

            // If we have an Effect, we may need to inflate the bounds to account for the effect's output.
            if (effect != null)
            {
                bounds = effect.GetRenderBounds(bounds);
            }
            
            //
            // Rasterize visual in its entirety. Banding is not useful at this point since
            // the resulting bands are all kept in memory anyway. Plus transformation is applied
            // to the band, which causes gaps between bands if they're rotated (bug 1562237).
            //
            // Banding is performed at GDIExporter layer just prior to sending images to the printer.
            //
            bitmap = Utility.RasterizeVisual(
                visual,
                bounds,
                visualToWorldTransform * inheritedTransformHint,
                out bitmapToVisualTransform
                );


            if (bitmap != null)
            {
                _dc.Push(bitmapToVisualTransform, null, 1.0, null, Rect.Empty, /*onePrimitive=*/true, null, null, null, EdgeMode.Unspecified);
                _dc.DrawImage(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height));
                _dc.Pop();
            }
            
            _dc.Pop();
        }

        /// <summary>
        /// Draw a GlyphRun.
        /// </summary>
        public void DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            if (glyphRun != null)
            {
                foreground = ReduceBrush(foreground, glyphRun.ComputeInkBoundingBox());

                // foreground may be null, but glyphrun may still be a hyperlink
                _dc.DrawGlyphRun(foreground, glyphRun);
            }
        }

        #endregion
    }
    #endregion

    internal static class GeometryHelper
    {
        const double FUZZ = 1e-6;           // Relative 0
        const double PI_OVER_180 = Math.PI / 180;  // PI/180

        //  Function: AcceptRadius
        //  Synopsis: Accept one radius
        //  Return:   false if the radius is too small compared to the chord length
        public static bool
        AcceptRadius(
            double rHalfChord2,    // (1/2 chord length)squared
            double rFuzz2,         // Squared fuzz
            ref double rRadius)   // The radius to accept (or not)
        {
            Debug.Assert(rHalfChord2 >= rFuzz2);   // Otherewise we have no guarantee that the radius is not 0,
            // and we need to divide by the radius
            bool fAccept = (rRadius * rRadius > rHalfChord2 * rFuzz2);

            if (fAccept)
            {
                if (rRadius < 0)
                {
                    rRadius = 0;
                }
            }

            return fAccept;
        }

        public static Point Add(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        public static Point Sub(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        // Dot Product
        public static double DotProduct(Point a, Point b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Determinant(Point a, Point b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        //+-------------------------------------------------------------------------------------------------

        //
        //  Function: GetArcAngle
        //
        //  Synopsis: Get the number of Bezier arcs, and sine & cosine of each
        //
        //  Notes:    This is a private utility used by ArcToBezier
        //            We break the arc into pieces so that no piece will span more than 90 degrees.
        //            The input points are on the unit circle
        //
        //--------------------------------------------------------------------------------------------------
        public static void
        GetArcAngle(
            Point ptStart,      // Start point
            Point ptEnd,        // End point
            bool fIsLargeArc,     // Choose the larger of the 2 possible arcs if TRUE
            SweepDirection eSweepDirection,      // Direction n which to sweep the arc.
            out double rCosArcAngle, // Cosine of a the sweep angle of one arc piece
            out double rSinArcAngle, // Sine of a the sweep angle of one arc piece
            out int cPieces)      // Out: The number of pieces
        {
            double rAngle;

            // The points are on the unit circle, so:
            rCosArcAngle = DotProduct(ptStart, ptEnd);
            rSinArcAngle = Determinant(ptStart, ptEnd);

            if (rCosArcAngle >= 0)
            {
                if (fIsLargeArc)
                {
                    // The angle is between 270 and 360 degrees, so
                    cPieces = 4;
                }
                else
                {
                    // The angle is between 0 and 90 degrees, so
                    cPieces = 1;
                    return; // We already have the cosine and sine of the angle
                }
            }
            else
            {
                if (fIsLargeArc)
                {
                    // The angle is between 180 and 270 degrees, so
                    cPieces = 3;
                }
                else
                {
                    // The angle is between 90 and 180 degrees, so
                    cPieces = 2;
                }
            }

            // We have to chop the arc into the computed number of pieces.  For cPieces=2 and 4 we could
            // have uses the half-angle trig formulas, but for cPieces=3 it requires solving a cubic 
            // equation; the performance difference is not worth the extra code, so we'll get the angle,
            // divide it, and get its sine and cosine.

            Debug.Assert(cPieces > 0);
            rAngle = Math.Atan2(rSinArcAngle, rCosArcAngle);

            if (eSweepDirection == SweepDirection.Clockwise)
            {
                if (rAngle < 0)
                {
                    rAngle += Math.PI * 2;
                }
            }
            else
            {
                if (rAngle > 0)
                {
                    rAngle -= Math.PI * 2;
                }
            }

            rAngle /= cPieces;
            rCosArcAngle = Math.Cos(rAngle);
            rSinArcAngle = Math.Sin(rAngle);
        }

        /*****************************************************************************\
        *
        * Function Description:
        *
        * Get the distance from a circular arc's endpoints to the control points of the
        * Bezier arc that approximates it, as a fraction of the arc's radius.
        *
        * Since the result is relative to the arc's radius, it depends strictly on the
        * arc's angle. The arc is assumed to be of 90 degrees of less, so the angle is
        * determined by the cosine of that angle, which is derived from rDot = the dot 
        * product of two radius vectors.  We need the Bezier curve that agrees with
        * the arc's points and tangents at the ends and midpoint.  Here we compute the
        * distance from the curve's endpoints to its control points.
        * 
        * Since we are looking for the relative distance, we can work on the unit
        * circle. Place the center of the circle at the origin, and put the X axis as
        * the bisector between the 2 vectors.  Let a be the angle between the vectors. 
        * Then the X coordinates of the 1st & last points are cos(a/2).  Let x be the X
        * coordinate of the 2nd & 3rd points.  At t=1/2 we have a point at (1,0).
        * But the terms of the polynomial there are all equal:
        *
        *           (1-t)^3 = t*(1-t)^2 = 2^2*(1-t) = t^3 = 1/8,
        *           
        * so from the Bezier formula there we have: 
        *
        *           1 = (1/8) * (cos(a/2) + 3x + 3x + cos(a/2)), 
        * hence
        *           x = (1 - cos(a/2)) / 3
        * 
        * The X difference between that and the 1st point is:
        *
        *           DX = x - cos(a/2) = 4(1 - cos(a/2)) / 3.
        *
        * But DX = distance / sin(a/2), hence the distance is
        *
        *           dist = (4/3)*(1 - cos(a/2)) / sin(a/2).
        *
        * Created:  5/29/2001 MichKa
        *
        /*****************************************************************************/
        public static double
        GetBezierDistance(  // Return the distance as a fraction of the radius
            double rDot,    // In: The dot product of the two radius vectors
            double rRadius) // In: The radius of the arc's circle (optional=1)
        {
            double rRadSquared = rRadius * rRadius;  // Squared radius

            Debug.Assert(rDot >= -rRadSquared * .1);  // angle < 90 degrees
            Debug.Assert(rDot <= rRadSquared * 1.1);  // as dot product of 2 radius vectors

            double rDist = 0;   // Acceptable fallback value

            /* Rather than the angle a, we are given rDot = R^2 * cos(a), so we 
                multiply top and bottom by R:

                                dist = (4/3)*(R - Rcos(a/2)) / Rsin(a/2)

                and use some trig:
                                    __________
                        cos(a/2) = \/1 + cos(a) / 2
                                        ________________         __________
                        R*cos(a/2) = \/R^2 + R^2 cos(a) / 2 = \/R^2 + rDot / 2 */

            double rCos = (rRadSquared + rDot) / 2;   // =(R*cos(a))^2
            if (rCos < 0)   // Shouldn't happen but dist=0 will work
            {
                return rDist;
            }
            //                 __________________
            //  R*sin(a/2) = \/R^2 - R^2 cos(a/2)  

            double rSin = rRadSquared - rCos;         // =(R*sin(a))^2
            if (rSin <= 0)
                // 0 angle, we shouldn't be rounding the corner, but dist=0 is OK
                return rDist;

            rSin = Math.Sqrt(rSin); //   = R*cos(a)
            rCos = Math.Sqrt(rCos); //   = R*sin(a)

            rDist = 4 * (rRadius - rCos) / 3;
            if (rDist <= rSin * FUZZ)
            {
                rDist = 0;
            }
            else
            {
                rDist = 4 * (rRadius - rCos) / rSin / 3;
            }

            return rDist;
        }

        //+-------------------------------------------------------------------------------------------------
        //
        //  Function: ArcToBezier
        //
        //  Synopsis: Compute the Bezier approximation of an arc
        //
        //  Notes:    This utilitycomputes the Bezier approximation for an elliptical arc as it is defined
        //            in the SVG arc spec. The ellipse from which the arc is carved is axis-aligned in its
        //            own coordinates, and defined there by its x and y radii. The rotation angle defines 
        //            how the ellipse's axes are rotated relative to our x axis. The start and end points
        //            define one of 4 possible arcs; the sweep and large-arc flags determine which one of 
        //            these arcs will be chosen. See SVG spec for details.
        //
        //            Returning cPieces = 0 indicates a line instead of an arc
        //                      cPieces = -1 indicates that the arc degenerates to a point 
        //
        //--------------------------------------------------------------------------------------------------
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        public static PointCollection ArcToBezier(
            double xStart,     // X coordinate of the last point
            double yStart,     // Y coordinate of the last point
            double xRadius,    // The ellipse's X radius
            double yRadius,    // The ellipse's Y radius
            double rRotation,  // Rotation angle of the ellipse's x axis
            bool fIsLargeArc,  // Choose the larger of the 2 possible arcs if TRUE
            SweepDirection eSweepDirection,   // Sweep the arc while increasing the angle if TRUE
            double xEnd,       // X coordinate of the last point
            double yEnd,       // Y coordinate of the last point
            out int cPieces)    // The number of output Bezier curves
        {
            double rCosArcAngle, rSinArcAngle, xCenter, yCenter, r, rBezDist;
            Point vecToBez1, vecToBez2;
            Matrix matToEllipse;

            double rFuzz2 = FUZZ * FUZZ;
            bool fZeroCenter = false;

            cPieces = -1;

            // In the following, the line segment between between the arc's start and 
            // end points is referred to as "the chord".

            // Transform 1: Shift the origin to the chord's midpoint
            double x = (xEnd - xStart) / 2;
            double y = (yEnd - yStart) / 2;

            double rHalfChord2 = x * x + y * y;     // (half chord length)^2

            // Degenerate case: single point
            if (rHalfChord2 < rFuzz2)
            {
                // The chord degeneartes to a point, the arc will be ignored
                return null;
            }

            // Degenerate case: straight line
            if (!AcceptRadius(rHalfChord2, rFuzz2, ref xRadius) ||
                !AcceptRadius(rHalfChord2, rFuzz2, ref yRadius))
            {
                // We have a zero radius, add a straight line segment instead of an arc
                cPieces = 0;
                return null;
            }

            // Transform 2: Rotate to the ellipse's coordinate system
            rRotation = -rRotation * PI_OVER_180;

            double rCos = Math.Cos(rRotation);
            double rSin = Math.Sin(rRotation);

            r = x * rCos - y * rSin;
            y = x * rSin + y * rCos;
            x = r;

            // Transform 3: Scale so that the ellipse will become a unit circle
            x /= xRadius;
            y /= yRadius;

            // We get to the center of that circle along a verctor perpendicular to the chord   
            // from the origin, which is the chord's midpoint. By Pythagoras, the length of that
            // vector is sqrt(1 - (half chord)^2).

            rHalfChord2 = x * x + y * y;   // now in the circle coordinates   

            if (rHalfChord2 > 1)
            {
                // The chord is longer than the circle's diameter; we scale the radii uniformly so 
                // that the chord will be a diameter. The center will then be the chord's midpoint,
                // which is now the origin.
                r = Math.Sqrt(rHalfChord2);
                xRadius *= r;
                yRadius *= r;
                xCenter = yCenter = 0;
                fZeroCenter = true;

                // Adjust the unit-circle coordinates x and y
                x /= r;
                y /= r;
            }
            else
            {
                // The length of (-y,x) or (x,-y) is sqrt(rHalfChord2), and we want a vector
                // of length sqrt(1 - rHalfChord2), so we'll multiply it by:
                r = Math.Sqrt((1 - rHalfChord2) / rHalfChord2);
                if (fIsLargeArc != (eSweepDirection == SweepDirection.Clockwise))
                // Going to the center from the origin=chord-midpoint
                {
                    // in the direction of (-y, x)
                    xCenter = -r * y;
                    yCenter = r * x;
                }
                else
                {
                    // in the direction of (y, -x)
                    xCenter = r * y;
                    yCenter = -r * x;
                }
            }

            // Transformation 4: shift the origin to the center of the circle, which then becomes
            // the unit circle. Since the chord's midpoint is the origin, the start point is (-x, -y)
            // and the endpoint is (x, y).
            Point ptStart = new Point(-x - xCenter, -y - yCenter);
            Point ptEnd = new Point(x - xCenter, y - yCenter);

            // Set up the matrix that will take us back to our coordinate system.  This matrix is
            // the inverse of the combination of transformation 1 thru 4.
            matToEllipse = new Matrix(rCos * xRadius, -rSin * xRadius,
                                      rSin * yRadius, rCos * yRadius,
                                      (xEnd + xStart) / 2, (yEnd + yStart) / 2);

            if (!fZeroCenter)
            {
                // Prepend the translation that will take the origin to the circle's center
                matToEllipse.OffsetX += (matToEllipse.M11 * xCenter + matToEllipse.M21 * yCenter);
                matToEllipse.OffsetY += (matToEllipse.M12 * xCenter + matToEllipse.M22 * yCenter);
            }

            // Get the sine & cosine of the angle that will generate the arc pieces
            GetArcAngle(ptStart, ptEnd, fIsLargeArc, eSweepDirection, out rCosArcAngle, out rSinArcAngle, out cPieces);

            // Get the vector to the first Bezier control point
            rBezDist = GetBezierDistance(rCosArcAngle, 1);

            if (eSweepDirection == SweepDirection.Counterclockwise)
            {
                rBezDist = -rBezDist;
            }

            vecToBez1 = new Point(-rBezDist * ptStart.Y, rBezDist * ptStart.X);

            PointCollection rslt = new PointCollection();

            // Add the arc pieces, except for the last
            for (int i = 1; i < cPieces; i++)
            {
                // Get the arc piece's endpoint
                Point ptPieceEnd = new Point(ptStart.X * rCosArcAngle - ptStart.Y * rSinArcAngle,
                                    ptStart.X * rSinArcAngle + ptStart.Y * rCosArcAngle);
                vecToBez2 = new Point(-rBezDist * ptPieceEnd.Y, rBezDist * ptPieceEnd.X);

                rslt.Add(matToEllipse.Transform(Add(ptStart, vecToBez1)));
                rslt.Add(matToEllipse.Transform(Sub(ptPieceEnd, vecToBez2)));
                rslt.Add(matToEllipse.Transform(ptPieceEnd));

                // Move on to the next arc
                ptStart = ptPieceEnd;
                vecToBez1 = vecToBez2;
            }

            // Last arc - we know the endpoint
            vecToBez2 = new Point(-rBezDist * ptEnd.Y, rBezDist * ptEnd.X);

            rslt.Add(matToEllipse.Transform(Add(ptStart, vecToBez1)));
            rslt.Add(matToEllipse.Transform(Sub(ptEnd, vecToBez2)));
            rslt.Add(new Point(xEnd, yEnd));

            return rslt;
        }
    };

    /// <summary>
    /// IMetroDrawingContext implementation to convert VisualBrush to DrawingBrush to
    /// reduce the number of Brush types we need to handle.
    /// </summary>
    internal class DrawingFlattenDrawingContext : IMetroDrawingContext
    {
        #region Public Properties

        private DrawingContext _context = null;

        // Records number of DrawingContext.Push calls part of each DrawingFlattenDrawingContext.Push call
        // for use in stack popping.
        private Stack _push = new Stack();

        #endregion

        #region Constructors

        public DrawingFlattenDrawingContext(DrawingContext context)
        {
            Debug.Assert(context != null);

            _context = context;
        }

        #endregion

        #region IMetroDrawingContext Members

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            _context.DrawGeometry(brush, pen, geometry);
        }

        public void DrawImage(ImageSource image, Rect rectangle)
        {
            _context.DrawImage(image, rectangle);
        }

        public void DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            _context.DrawGlyphRun(foreground, glyphRun);
        }

        public void Push(
            Matrix transform,
            Geometry clip,
            double opacity,
            Brush opacityMask,
            Rect maskBounds,
            bool onePrimitive,

            // serialization attributes
            String nameAttr,
            Visual node,
            Uri navigateUri,
            EdgeMode edgeMode
            )
        {
            opacity = Utility.NormalizeOpacity(opacity);

            int pushCount = 0;

            if (!transform.IsIdentity)
            {
                _context.PushTransform(new MatrixTransform(transform));
                pushCount++;
            }

            if (clip != null)
            {
                _context.PushClip(clip);
                pushCount++;
            }

            if (!Utility.IsOpaque(opacity))
            {
                _context.PushOpacity(opacity);
                pushCount++;
            }

            if (opacityMask != null)
            {
                _context.PushOpacityMask(opacityMask);
                pushCount++;
            }

            _push.Push(pushCount);
        }

        public void Pop()
        {
            int popCount = (int)_push.Pop();

            for (int index = 0; index < popCount; index++)
            {
                _context.Pop();
            }
        }

        public void Comment(string message)
        {
        }

        #endregion
    }
}

