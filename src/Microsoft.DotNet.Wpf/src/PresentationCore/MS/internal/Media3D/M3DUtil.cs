// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Collection of utility classes for the Media3D namespace.
//

using MS.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MS.Internal.Media3D
{
    internal static class M3DUtil
    {
        // Returns the interpolated 3D point from the given positions and barycentric
        // coordinate.
        //
        // NOTE: v0-v2 and barycentric are passed by ref for performance.  They are
        //       not modified.
        //
        internal static Point3D Interpolate(ref Point3D v0, ref Point3D v1, ref Point3D v2, ref Point barycentric)
        {
            double v = barycentric.X;
            double w = barycentric.Y;
            double u = 1 - v - w;
                
            return new Point3D(u*v0.X + v*v1.X + w*v2.X,
                               u*v0.Y + v*v1.Y + w*v2.Y,
                               u*v0.Z + v*v1.Z + w*v2.Z);
        }
        
        // Helper method for compiting the bounds of a set of points.  The given point
        // is added to the bounds of the given Rect3D.  The point/bounds are both passed
        // by reference for perf.  Only the bounds may be modified.
        private static void AddPointToBounds(ref Point3D point, ref Rect3D bounds)
        {
            Debug.Assert(!bounds.IsEmpty,
                "Caller should construct the Rect3D from the first point before calling this method.");

            if (point.X < bounds.X)
            {
                bounds.SizeX += (bounds.X - point.X);
                bounds.X = point.X;
            }
            else if (point.X > (bounds.X + bounds.SizeX))
            {
                bounds.SizeX = point.X - bounds.X;
            }

            if (point.Y < bounds.Y)
            {
                bounds.SizeY += (bounds.Y - point.Y);
                bounds.Y = point.Y;
            }
            else if (point.Y > (bounds.Y + bounds.SizeY))
            {
                bounds.SizeY = point.Y - bounds.Y;
            }

            if (point.Z < bounds.Z)
            {
                bounds.SizeZ += (bounds.Z - point.Z);
                bounds.Z = point.Z;
            }
            else if (point.Z > (bounds.Z + bounds.SizeZ))
            {
                bounds.SizeZ = point.Z - bounds.Z;
            }

#if NEVER
            // Because we do not store rectangles as TLRB (+ another dimension in 3D)
            // we need to compute SizeX/Y/Z which involves subtraction and introduces
            // cancelation so this assert isn't accurate.
            Debug.Assert(bounds.Contains(point),
                "Error detect - bounds did not contain point on exit.");
#endif
        }

        // Computes an axis aligned bounding box that contains the given set of points.
        internal static Rect3D ComputeAxisAlignedBoundingBox(Point3DCollection positions)
        {
            if (positions != null)
            {
                FrugalStructList<Point3D> points = positions._collection;

                if (points.Count != 0)
                {
                    Point3D p = points[0];
                    Rect3D newBounds = new Rect3D(p.X, p.Y, p.Z, 0, 0, 0);

                    for(int i = 1; i < points.Count; i++)
                    {
                        p = points[i];

                        M3DUtil.AddPointToBounds(ref p, ref newBounds);
                    }

                    return newBounds;
                }
            }

            return Rect3D.Empty;
        }

        // Returns a new axis aligned bounding box that contains the old
        // bounding box post the given transformation.
        internal static Rect3D ComputeTransformedAxisAlignedBoundingBox(/* IN */ ref Rect3D originalBox, Transform3D transform)
        {
            if (transform == null || transform == Transform3D.Identity)
            {
                return originalBox;
            }

            Matrix3D matrix = transform.Value;

            return ComputeTransformedAxisAlignedBoundingBox(ref originalBox, ref matrix);
        }

        // Returns a new axis aligned bounding box that contains the old
        // bounding box post the given transformation.
        internal static Rect3D ComputeTransformedAxisAlignedBoundingBox( /* IN */ ref Rect3D originalBox, /* IN */ ref Matrix3D matrix)
        {
            if (originalBox.IsEmpty)
            {
                return originalBox;
            }

            if (matrix.IsAffine)
            {
                return ComputeTransformedAxisAlignedBoundingBoxAffine(ref originalBox, ref matrix);
            }
            else
            {
                return ComputeTransformedAxisAlignedBoundingBoxNonAffine(ref originalBox, ref matrix);
            }
        }

        // CTAABB for an affine transforms
        internal static Rect3D ComputeTransformedAxisAlignedBoundingBoxAffine(/* IN */ ref Rect3D originalBox, /* IN */ ref Matrix3D matrix)
        {
            Debug.Assert(matrix.IsAffine);
            
            // Based on Arvo's paper "Transforming Axis-Aligned Bounding Boxes" 
            // from the original Graphics Gems book. Specifically, this code
            // is based on Figure 1 which is for a box stored as min and
            // max points. Our bounding boxes are stored as a min point and
            // a diagonal so we'll convert when needed. Also, we have row
            // vectors.
            //
            // Mapping Arvo's variables to ours:
            // A - the untransformed box (originalBox) 
            // B - the transformed box (what we return at the end)
            // M - the rotation + scale (matrix.Mji)
            // T - the translation (matrix.Offset?)
            //
            // for i = 1 ... 3
            //     Bmin_i = Bmax_i = T_i
            //         for j = 1 ... 3
            //             a = M_ij * Amin_j
            //             b = M_ij * Amax_j
            //             Bmin_i += min(a, b)
            //             Bmax_i += max(a, b)
            //
            // Matrix3D doesn't have indexers because they're too slow so we'll
            // have to unroll the loops. A complete unroll of both loops was
            // found to be the fastest.

            double oldMaxX = originalBox.X + originalBox.SizeX;
            double oldMaxY = originalBox.Y + originalBox.SizeY;
            double oldMaxZ = originalBox.Z + originalBox.SizeZ;

            // i = 1 (X)
            double newMinX = matrix.OffsetX;
            double newMaxX = matrix.OffsetX;
            {
                // i = 1 (X), j = 1 (X)
                double a = matrix.M11 * originalBox.X;
                double b = matrix.M11 * oldMaxX;
                if (b > a)
                {
                    newMinX += a;
                    newMaxX += b;
                }
                else
                {
                    newMinX += b;
                    newMaxX += a;
                }
                
                // i = 1 (X), j = 2 (Y)
                a = matrix.M21 * originalBox.Y;
                b = matrix.M21 * oldMaxY;
                if (b > a)
                {
                    newMinX += a;
                    newMaxX += b;
                }
                else
                {
                    newMinX += b;
                    newMaxX += a;
                }
                
                // i = 1 (X), j = 3 (Z)
                a = matrix.M31 * originalBox.Z;
                b = matrix.M31 * oldMaxZ;
                if (b > a)
                {
                    newMinX += a;
                    newMaxX += b;
                }
                else
                {
                    newMinX += b;
                    newMaxX += a;
                }
            }

            // i = 2 (Y)
            double newMinY = matrix.OffsetY;
            double newMaxY = matrix.OffsetY;
            {
                // i = 2 (Y), j = 1 (X)
                double a = matrix.M12 * originalBox.X;
                double b = matrix.M12 * oldMaxX;
                if (b > a)
                {
                    newMinY += a;
                    newMaxY += b;
                }
                else
                {
                    newMinY += b;
                    newMaxY += a;
                }
                
                // i = 2 (Y), j = 2 (Y)
                a = matrix.M22 * originalBox.Y;
                b = matrix.M22 * oldMaxY;
                if (b > a)
                {
                    newMinY += a;
                    newMaxY += b;
                }
                else
                {
                    newMinY += b;
                    newMaxY += a;
                }
                
                // i = 2 (Y), j = 3 (Z)
                a = matrix.M32 * originalBox.Z;
                b = matrix.M32 * oldMaxZ;
                if (b > a)
                {
                    newMinY += a;
                    newMaxY += b;
                }
                else
                {
                    newMinY += b;
                    newMaxY += a;
                } 
            }

            // i = 3 (Z)
            double newMinZ = matrix.OffsetZ;
            double newMaxZ = matrix.OffsetZ;
            {
                // i = 3 (Z), j = 1 (X)
                double a = matrix.M13 * originalBox.X;
                double b = matrix.M13 * oldMaxX;
                if (b > a)
                {
                    newMinZ += a;
                    newMaxZ += b;
                }
                else
                {
                    newMinZ += b;
                    newMaxZ += a;
                }
                
                // i = 3 (Z), j = 2 (Y)
                a = matrix.M23 * originalBox.Y;
                b = matrix.M23 * oldMaxY;
                if (b > a)
                {
                    newMinZ += a;
                    newMaxZ += b;
                }
                else
                {
                    newMinZ += b;
                    newMaxZ += a;
                }
                
                // i = 3 (Z), j = 3 (Z)
                a = matrix.M33 * originalBox.Z;
                b = matrix.M33 * oldMaxZ;
                if (b > a)
                {
                    newMinZ += a;
                    newMaxZ += b;
                }
                else
                {
                    newMinZ += b;
                    newMaxZ += a;
                }
            }
           
            return new Rect3D(newMinX, newMinY, newMinZ, newMaxX - newMinX, newMaxY - newMinY, newMaxZ - newMinZ);
        }

        // CTAABB for non-affine transformations
        internal static Rect3D ComputeTransformedAxisAlignedBoundingBoxNonAffine(/* IN */ ref Rect3D originalBox, /* IN */ ref Matrix3D matrix)
        {
            Debug.Assert(!matrix.IsAffine);
            
            double x1 = originalBox.X;
            double y1 = originalBox.Y;
            double z1 = originalBox.Z;
            double x2 = originalBox.X + originalBox.SizeX;
            double y2 = originalBox.Y + originalBox.SizeY;
            double z2 = originalBox.Z + originalBox.SizeZ;

            Point3D[] points = new Point3D[] {
                new Point3D(x1, y1, z1),
                new Point3D(x1, y1, z2),
                new Point3D(x1, y2, z1),
                new Point3D(x1, y2, z2),
                new Point3D(x2, y1, z1),
                new Point3D(x2, y1, z2),
                new Point3D(x2, y2, z1),
                new Point3D(x2, y2, z2),
            };

            matrix.Transform(points);

            Point3D p = points[0];
            Rect3D newBounds = new Rect3D(p.X, p.Y, p.Z, 0, 0, 0);

            // Traverse the entire mesh and compute bounding box.
            for(int i = 1; i < points.Length; i++)
            {
                p = points[i];

                AddPointToBounds(ref p, ref newBounds);
            }

            return newBounds;
        }

        // Returns the aspect ratio of the given size.
        internal static double GetAspectRatio(Size viewSize)
        {
            return viewSize.Width / viewSize.Height;
        }

        // Normalizes the point in the given size to the range [-1, 1].
        internal static Point GetNormalizedPoint(Point point, Size size)
        {
            return new Point(
                ((2*point.X)/size.Width) - 1,
                -(((2*point.Y)/size.Height) - 1));
        }

        internal static double RadiansToDegrees(double radians)
        {
            return radians*(180.0/Math.PI);
        }

        internal static double DegreesToRadians(double degrees)
        {
            return degrees*(Math.PI/180.0);
        }

        internal static Matrix3D GetWorldToViewportTransform3D(Camera camera, Rect viewport)
        {
            Debug.Assert(camera != null, "Caller is responsible for ensuring camera is not null.");
            
            return camera.GetViewMatrix() *
                camera.GetProjectionMatrix(M3DUtil.GetAspectRatio(viewport.Size)) *
                M3DUtil.GetHomogeneousToViewportTransform3D(viewport);
        }

        /// <summary>
        /// GetHomogeneousToViewportTransform3D.
        ///
        /// Returns a matrix that performs the coordinate system change from
        ///
        ///             1
        ///             |
        ///  -1 --------|------ 1
        ///             |
        ///             -1
        ///
        ///
        ///   (Viewport.X, Viewport.Y) ---------- (Viewport.X + Viewport.Width, 0)
        ///               |
        ///               | 
        ///               |
        ///   (Viewport.X, Viewport.Y + Viewport.Height)
        ///
        /// In other words, the viewport transform stretches the normalized coordinate
        /// system of [(-1, 1):(1, -1)] into the Viewport.
        /// </summary>
        internal static Matrix3D GetHomogeneousToViewportTransform3D(Rect viewport)
        {
            // Matrix3D scaling = new Matrix3D(
            //     1,  0, 0, 0, 
            //     0, -1, 0, 0,
            //     0,  0, 1, 0,
            //     0,  0, 0, 1);
            // 
            // Matrix3D translation = new Matrix3D(
            //     1, 0, 0, 0,
            //     0, 1, 0, 0,
            //     0, 0, 1, 0,
            //     1, 1, 0, 1);
            // 
            // scaling * translation 
            //
            // 
            // == 
            //
            // 1,  0, 0, 0,
            // 0, -1, 0, 0,
            // 0,  0, 1, 0,
            // 1,  1, 0, 1
            // 
            // 
            // Matrix3D viewportScale = new Matrix3D(
            //     Viewport.Width / 2, 0,                   0, 0,
            //     0,                  Viewport.Height / 2, 0, 0,
            //     0,                  0,                   1, 0,
            //     0,                  0,                   0, 1);
            // 
            // 
            // * viewportScale
            // 
            // ==
            // 
            // vw/2, 0,     0, 0,
            // 0,    -vh/2, 0, 0,
            // 0,    0,     1, 0,
            // vw/2, vh/2,  0, 1,
            // 
            // 
            // Matrix3D viewportOffset = new Matrix3D(
            //     1, 0, 0, 0,
            //     0, 1, 0, 0,
            //     0, 0, 1, 0,
            //     Viewport.X, Viewport.Y, 0, 1);
            // 
            // 
            // * viewportOffset
            // 
            // ==
            // 
            // vw/2, 0,     0, 0,
            // 0,    -vh/2, 0, 0,
            // 0,    0,     1, 0,
            // vw/2+vx, vh/2+vy, 0, 1

            double sx = viewport.Width / 2; // half viewport width
            double sy = viewport.Height / 2; // half viewport height
            double tx = viewport.X + sx;
            double ty = viewport.Y + sy;

            return new Matrix3D(
                sx,  0, 0, 0,
                0 , -sy, 0, 0,
                0 ,  0, 1, 0,
                tx, ty, 0, 1);                
}

        /// <summary>
        /// Same as GetHomogeneousToViewportTransform3D but returns a 2D matrix.
        /// For detailed comments see: GetHomogeneousToViewportTransform3D
        /// </summary>
        internal static Matrix GetHomogeneousToViewportTransform(Rect viewport)
        {
            double sx = viewport.Width / 2; // half viewport width
            double sy = viewport.Height / 2; // half viewport height
            double tx = viewport.X + sx;
            double ty = viewport.Y + sy;

            return new Matrix(
                sx,  0,
                0 , -sy,
                tx, ty);                
        }

        /// <summary>
        /// Gets the object space to world space transformation for the given Visual3D
        /// </summary>
        /// <param name="visual">The visual whose world space transform should be found</param>
        /// <returns>The world space transformation</returns>
        internal static Matrix3D GetWorldTransformationMatrix(Visual3D visual)
        {
            Viewport3DVisual ignored;

            return GetWorldTransformationMatrix(visual, out ignored);
        }

        /// <summary>
        /// Gets the object space to world space transformation for the given Visual3D
        /// </summary>
        /// <param name="visual3DStart">The visual whose world space transform should be found</param>
        /// <param name="viewport">The containing Viewport3D for the Visual3D</param>
        /// <returns>The world space transformation</returns>
        internal static Matrix3D GetWorldTransformationMatrix(Visual3D visual3DStart, out Viewport3DVisual viewport)
        {
            DependencyObject dependencyObject = visual3DStart;
            Matrix3D worldTransform = Matrix3D.Identity;
       
            while (dependencyObject != null)
            {
                Visual3D visual3D = dependencyObject as Visual3D;

                // we reached the top
                if (visual3D == null) 
                {
                    break;
                }
                
                Transform3D transform = (Transform3D)visual3D.GetValue(Visual3D.TransformProperty);

                if (transform != null)
                {
                    transform.Append(ref worldTransform);
                }

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);      
            }

            if (dependencyObject != null)
            {
                viewport = (Viewport3DVisual)dependencyObject;
            }
            else
            {
                viewport = null;
            }

            return worldTransform;
        }

        /// <summary>
        /// Computes the transformation matrix to go from a 3D point in the given Visual3D's coordinate space out in to
        /// the Viewport3DVisual.
        /// </summary>
        internal static bool TryTransformToViewport3DVisual(Visual3D visual3D, out Viewport3DVisual viewport, out Matrix3D matrix)
        {
            matrix = GetWorldTransformationMatrix(visual3D, out viewport);

            if (viewport != null)
            {
                matrix *= GetWorldToViewportTransform3D(viewport.Camera, viewport.Viewport);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Function tests to see if the given texture coordinate point p is contained within the 
        /// given triangle.  If it is it returns the 3D point corresponding to that intersection.
        /// </summary>
        /// <param name="p">The point to test</param>
        /// <param name="triUVVertices">The texture coordinates of the triangle</param>
        /// <param name="tri3DVertices">The 3D coordinates of the triangle</param>
        /// <param name="inters3DPoint">The 3D point of intersection</param>
        /// <returns>True if the point is in the triangle, false otherwise</returns>
        internal static bool IsPointInTriangle(Point p, Point[] triUVVertices, Point3D[] tri3DVertices, out Point3D inters3DPoint)
        {
            double denom = 0.0;
            inters3DPoint = new Point3D();

            //
            // get the barycentric coordinates and then use these to test if the point is in the triangle
            // any standard math reference on barycentric coordinates will give the derivation for the below
            // parameters.
            //
            double A = triUVVertices[0].X - triUVVertices[2].X;
            double B = triUVVertices[1].X - triUVVertices[2].X;
            double C = triUVVertices[2].X - p.X;
            double D = triUVVertices[0].Y - triUVVertices[2].Y;
            double E = triUVVertices[1].Y - triUVVertices[2].Y;
            double F = triUVVertices[2].Y - p.Y;

            denom = (A * E - B * D);
            if (denom == 0) 
            {
                return false;
            }
            double lambda1 = (B * F - C * E) / denom;

            denom = (B * D - A * E);
            if (denom == 0) 
            {
                return false;
            }
            double lambda2 = (A * F - C * D) / denom;

            if (lambda1 < 0 || lambda1 > 1 || 
                lambda2 < 0 || lambda2 > 1 || 
                (lambda1 + lambda2) > 1) 
            {
                return false;
            }

            inters3DPoint = (Point3D)(lambda1 * (Vector3D)tri3DVertices[0] +
                                      lambda2 * (Vector3D)tri3DVertices[1] +
                                      (1.0f - lambda1 - lambda2) * (Vector3D)tri3DVertices[2]);

            return true;
        }
    }
}
