// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// This is smaller than a double[3] and also value typed
    /// </summary>
    internal struct Weights
    {
        public double W1;
        public double W2;
        public double W3;
    }

    /// <summary>
    /// Interpolators used for rasterization
    /// </summary>
    internal class Interpolator
    {
        public static double WeightedSum(double d1, double d2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");
            return WeightedSum(d1, d2, 0, weights);
        }

        public static double WeightedSum(double d1, double d2, double d3, Weights weights)
        {
            return d1 * weights.W1 + d2 * weights.W2 + d3 * weights.W3;
        }

        public static Point WeightedSum(Point p1, Point p2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");
            return WeightedSum(p1, p2, new Point(), weights);
        }

        public static Point WeightedSum(Point p1, Point p2, Point p3, Weights weights)
        {
            return new Point(
                    WeightedSum(p1.X, p2.X, p3.X, weights),
                    WeightedSum(p1.Y, p2.Y, p3.Y, weights));
        }

        public static Point3D WeightedSum(Point3D p1, Point3D p2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");
            return WeightedSum(p1, p2, new Point3D(), weights);
        }

        public static Point3D WeightedSum(Point3D p1, Point3D p2, Point3D p3, Weights weights)
        {
            return new Point3D(
                    WeightedSum(p1.X, p2.X, p3.X, weights),
                    WeightedSum(p1.Y, p2.Y, p3.Y, weights),
                    WeightedSum(p1.Z, p2.Z, p3.Z, weights));
        }

        public static Point4D WeightedSum(Point4D p1, Point4D p2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");
            return WeightedSum(p1, p2, new Point4D(), weights);
        }

        public static Point4D WeightedSum(Point4D p1, Point4D p2, Point4D p3, Weights weights)
        {
            return new Point4D(
                    WeightedSum(p1.X, p2.X, p3.X, weights),
                    WeightedSum(p1.Y, p2.Y, p3.Y, weights),
                    WeightedSum(p1.Z, p2.Z, p3.Z, weights),
                    WeightedSum(p1.W, p2.W, p3.W, weights));
        }

        public static Vector3D WeightedSum(Vector3D v1, Vector3D v2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");

            // Does not normalize!
            return WeightedSum(v1, v2, new Vector3D(), weights);
        }

        public static Vector3D WeightedSum(Vector3D v1, Vector3D v2, Vector3D v3, Weights weights)
        {
            // Does not normalize!
            return new Vector3D(
                    WeightedSum(v1.X, v2.X, v3.X, weights),
                    WeightedSum(v1.Y, v2.Y, v3.Y, weights),
                    WeightedSum(v1.Z, v2.Z, v3.Z, weights));
        }

        public static Color WeightedSum(Color c1, Color c2, Weights weights)
        {
            System.Diagnostics.Debug.Assert(weights.W3 == 0, "Two component interpolation must be done with Weight3 == 0");
            return WeightedSum(c1, c2, new Color(), weights);
        }

        public static Color WeightedSum(Color c1, Color c2, Color c3, Weights weights)
        {
            double a, r, g, b;
            a = WeightedSum(ColorOperations.ByteToDouble(c1.A),
                             ColorOperations.ByteToDouble(c2.A),
                             ColorOperations.ByteToDouble(c3.A),
                             weights);

            r = WeightedSum(ColorOperations.ByteToDouble(c1.R),
                             ColorOperations.ByteToDouble(c2.R),
                             ColorOperations.ByteToDouble(c3.R),
                             weights);

            g = WeightedSum(ColorOperations.ByteToDouble(c1.G),
                             ColorOperations.ByteToDouble(c2.G),
                             ColorOperations.ByteToDouble(c3.G),
                             weights);

            b = WeightedSum(ColorOperations.ByteToDouble(c1.B),
                             ColorOperations.ByteToDouble(c2.B),
                             ColorOperations.ByteToDouble(c3.B),
                             weights);

            return ColorOperations.ColorFromArgb(a, r, g, b);
        }

        public static HdrColor WeightedSum(HdrColor c1, HdrColor c2, HdrColor c3, Weights weights)
        {
            return (c1 * weights.W1) + (c2 * weights.W2) + (c3 * weights.W3);
        }

        /// <summary>
        /// Finds the Weights that, when applied, will interpolate the two input points
        ///  to the xy plane located at the depth specified.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the two input points are on the same side of the xy plane.
        /// </exception>
        public static Weights GetWeightsToXYPlane(Point3D position1, Point3D position2, double planeDepth)
        {
            if (MathEx.SameSign(position1.Z - planeDepth, position2.Z - planeDepth))
            {
                throw new ArgumentException("The two vertices must be on opposite sides of the xy plane.");
            }

            //               xy plane
            //                  ^
            //        behind    |          ahead
            //                  |
            //  <-------1-------o------------2-----> z
            //                  |
            //                  v
            //
            // 1 = position1
            // 2 = position2
            // o = intersection at plane
            // (note that 1 and 2 are interchangeable because of our use of Math.Abs)
            //                         __
            // distance from x -> y == xy == abs( x-y )
            //                      __   __      __   __
            // p1's weight == 1 - ( 1o / 12 ) == 2o / 12 == abs( p2.Z - o ) / abs( p1.Z - p2.Z )
            // p2's weight == 1 - p1's weight

            double o = planeDepth;
            double length2o = Math.Abs(position2.Z - o);
            double length12 = Math.Abs(position1.Z - position2.Z);

            Weights weights = new Weights();
            weights.W1 = length2o / length12;
            weights.W2 = 1 - weights.W1;
            weights.W3 = 0;

            return weights;
        }
    }

    /// <summary>
    /// 3-Way Interpolator for triangle Vertex data.
    /// </summary>
    internal class TriangleInterpolator : Interpolator
    {
        public TriangleInterpolator(Triangle t)
        {
            vertex1 = t.vertex1;
            vertex2 = t.vertex2;
            vertex3 = t.vertex3;
            sourceTriangle = t;

            ComputeTriangleDistances();

            // Interpolating degenerate triangles is bad!  Don't do it!
            System.Diagnostics.Debug.Assert(!t.IsDegenerate);
        }

        public bool Contains(double x, double y)
        {
            // Alias this method
            bool contains = sourceTriangle.Contains(x, y);
            // ... and its side effects!
            pixelOnEdge = sourceTriangle.PixelOnEdge;
            return contains;
        }

        /// <summary>
        /// Does a perspective-correct, 3-way interpolation of vertex data based on projected point.
        /// Subclasses provide their own implementation.
        /// </summary>
        /// <param name="x">2D Screen-Space pixel X coordinate</param>
        /// <param name="y">2D Screen-Space pixel Y coordinate</param>
        /// <returns>Perspective-correct interpolated Vertex value for this point.</returns>
        public virtual Vertex GetVertex(double x, double y)
        {
            Vertex interpolation = new Vertex();

            Point3D currRasterPoint = new Point3D(x, y, 0);
            Weights screenWeights = ComputeScreenWeights(currRasterPoint);
            // Use screen weights to find current Z
            Point3D currProjectedPoint = new Point3D(x, y, WeightedSum(
                    vertex1.ProjectedPosition.Z,
                    vertex2.ProjectedPosition.Z,
                    vertex3.ProjectedPosition.Z,
                    screenWeights));
            interpolation.ProjectedPosition = currProjectedPoint;
            // Now that we have currProjectedPoint, we can go ahead and compute other weights
            Weights homogeneousWeights = ComputeHomogeneousWeights(currProjectedPoint);
            // Now we can use the perspectiveCorrectionFactor and the homogeneousWeights to
            // find the perspective-correct weights.
            Weights pcWeights = ComputePerspectiveCorrectWeights(homogeneousWeights);

            interpolation.W = 1.0 / WeightedSum(
                    vertex1.OneOverW,
                    vertex2.OneOverW,
                    vertex3.OneOverW,
                    homogeneousWeights);

            // Position ( Eye Space )
            interpolation.Position = WeightedSum(vertex1.Position, vertex2.Position, vertex3.Position, pcWeights);

            // Normal ( Eye Space )
            interpolation.Normal = MathEx.Normalize(
                    pcWeights.W1 * vertex1.Normal +
                    pcWeights.W2 * vertex2.Normal +
                    pcWeights.W3 * vertex3.Normal);

            // Color
            interpolation.Color = WeightedSum(vertex1.Color, vertex2.Color, vertex3.Color, pcWeights);

            // Color Error
            interpolation.ColorTolerance = WeightedSum(
                    vertex1.ColorTolerance,
                    vertex2.ColorTolerance,
                    vertex3.ColorTolerance,
                    pcWeights);

            if (pixelOnEdge)
            {
                // Since numerical precision makes these borderline cases inaccurate,
                // we set the tolerance of this pixel to ignore it.
                interpolation.ColorTolerance = ColorOperations.Add(
                    interpolation.ColorTolerance,
                    Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF));
            }

            // NOTE:
            //      x and y may not actually be contained by this triangle
            //      This can get us texture coordinates that are outside the [0,1] range

            // Texture Coordinates
            interpolation.TextureCoordinates = WeightedSum(
                    vertex1.TextureCoordinates,
                    vertex2.TextureCoordinates,
                    vertex3.TextureCoordinates,
                    pcWeights);

            // Precomputed Lighting for each material on this triangle
            if (vertex1.PrecomputedLight != null)
            {
                int materialCount = vertex1.PrecomputedLight.Length;

                interpolation.PrecomputedLight = new Color[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    interpolation.PrecomputedLight[i] = WeightedSum(
                            vertex1.PrecomputedLight[i],
                            vertex2.PrecomputedLight[i],
                            vertex3.PrecomputedLight[i],
                            pcWeights);
                }
            }

            // Precomputed Lighting tolerance for each material on this triangle
            if (vertex1.PrecomputedLightTolerance != null)
            {
                int materialCount = vertex1.PrecomputedLight.Length;

                interpolation.PrecomputedLightTolerance = new Color[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    interpolation.PrecomputedLightTolerance[i] = WeightedSum(
                            vertex1.PrecomputedLightTolerance[i],
                            vertex2.PrecomputedLightTolerance[i],
                            vertex3.PrecomputedLightTolerance[i],
                            pcWeights);
                }
            }

            return interpolation;
        }

        /// <summary>
        /// Does quick interpolation of UV coordinates for texture error lookup.
        /// </summary>
        /// <param name="x">2D Screen-Space pixel X coordinate</param>
        /// <param name="y">2D Screen-Space pixel Y coordinate</param>
        /// <returns>Perspective-correct interpolated UV values for this point.</returns>
        public Point GetTextureCoordinates(double x, double y)
        {
            // NOTE:
            //      x and y may not actually be contained by this triangle.
            //      This can get us texture coordinates that are outside the [0,1] range

            Point3D currRasterPoint = new Point3D(x, y, 0);
            Weights screenWeights = ComputeScreenWeights(currRasterPoint);
            // Use screen weights to find current Z
            Point3D currProjectedPoint = new Point3D(x, y, WeightedSum(
                    vertex1.ProjectedPosition.Z,
                    vertex2.ProjectedPosition.Z,
                    vertex3.ProjectedPosition.Z,
                    screenWeights));
            // Now that we have currProjectedPoint, we can go ahead and compute other weights
            Weights homogeneousWeights = ComputeHomogeneousWeights(currProjectedPoint);
            // Now we can use the perspectiveCorrectionFactor and the homogeneousWeights to
            // find the perspective-correct weights.
            Weights pcWeights = ComputePerspectiveCorrectWeights(homogeneousWeights);
            double u = WeightedSum(vertex1.U, vertex2.U, vertex3.U, pcWeights);
            double v = WeightedSum(vertex1.V, vertex2.V, vertex3.V, pcWeights);
            return new Point(u, v);
        }

        /// <summary>
        /// Compute non-perspective-corrected weightings of each triangle vertex, ignoring Z.
        /// </summary>
        protected Weights ComputeScreenWeights(Point3D currRasterPoint)
        {
            Weights screenWeights;

            // We're computing screen weights so we use the 2D point-to-line distance
            // NOTE: line winding order is important since PointToLineDistance2D is signed
            screenWeights.W1 = MathEx.DistanceFromLine2D(currRasterPoint, vertex2.ProjectedPosition, vertex3.ProjectedPosition) / vertex1.DistanceFromLine2D;
            screenWeights.W2 = MathEx.DistanceFromLine2D(currRasterPoint, vertex3.ProjectedPosition, vertex1.ProjectedPosition) / vertex2.DistanceFromLine2D;
            screenWeights.W3 = MathEx.DistanceFromLine2D(currRasterPoint, vertex1.ProjectedPosition, vertex2.ProjectedPosition) / vertex3.DistanceFromLine2D;

            return screenWeights;
        }

        /// <summary>
        /// Compute non-perspective-corrected weightings of each triangle vertex, in scaled homogeneous space.
        /// </summary>
        protected Weights ComputeHomogeneousWeights(Point3D currProjectedPoint)
        {
            Weights homogeneousWeights;

            // NOTE: line winding order is important since PointToLineDistance is signed
            homogeneousWeights.W1 = MathEx.DistanceFromLine(currProjectedPoint, vertex2.ProjectedPosition, vertex3.ProjectedPosition) / vertex1.DistanceFromLine;
            homogeneousWeights.W2 = MathEx.DistanceFromLine(currProjectedPoint, vertex3.ProjectedPosition, vertex1.ProjectedPosition) / vertex2.DistanceFromLine;
            homogeneousWeights.W3 = MathEx.DistanceFromLine(currProjectedPoint, vertex1.ProjectedPosition, vertex2.ProjectedPosition) / vertex3.DistanceFromLine;

            return homogeneousWeights;
        }

        /// <summary>
        /// Compute perspective-corrected weightings of each triangle vertex.
        /// </summary>
        protected Weights ComputePerspectiveCorrectWeights(Weights homogeneousWeights)
        {
            // We compute the perspective correction by interpolating 1/w
            // (see Real-Time Rendering, 2nd Ed., pg. 680)
            double perspectiveCorrectionFactor = WeightedSum(
                    vertex1.OneOverW,
                    vertex2.OneOverW,
                    vertex3.OneOverW,
                    homogeneousWeights);

            Weights unprojectedWeights;

            unprojectedWeights.W1 = (homogeneousWeights.W1 / vertex1.W) / perspectiveCorrectionFactor;
            unprojectedWeights.W2 = (homogeneousWeights.W2 / vertex2.W) / perspectiveCorrectionFactor;
            unprojectedWeights.W3 = (homogeneousWeights.W3 / vertex3.W) / perspectiveCorrectionFactor;

            return unprojectedWeights;
        }

        private void ComputeTriangleDistances()
        {
            // NOTE: since we are doing signed distance for interpolation, winding order is important
            vertex1.DistanceFromLine = MathEx.DistanceFromLine(vertex1.ProjectedPosition, vertex2.ProjectedPosition, vertex3.ProjectedPosition);
            vertex2.DistanceFromLine = MathEx.DistanceFromLine(vertex2.ProjectedPosition, vertex3.ProjectedPosition, vertex1.ProjectedPosition);
            vertex3.DistanceFromLine = MathEx.DistanceFromLine(vertex3.ProjectedPosition, vertex1.ProjectedPosition, vertex2.ProjectedPosition);
            vertex1.DistanceFromLine2D = MathEx.DistanceFromLine2D(vertex1.ProjectedPosition, vertex2.ProjectedPosition, vertex3.ProjectedPosition);
            vertex2.DistanceFromLine2D = MathEx.DistanceFromLine2D(vertex2.ProjectedPosition, vertex3.ProjectedPosition, vertex1.ProjectedPosition);
            vertex3.DistanceFromLine2D = MathEx.DistanceFromLine2D(vertex3.ProjectedPosition, vertex1.ProjectedPosition, vertex2.ProjectedPosition);
        }

        protected Vertex vertex1;
        protected Vertex vertex2;
        protected Vertex vertex3;

        protected Triangle sourceTriangle;
        protected bool pixelOnEdge;
    }

    /// <summary>
    /// Interpolator for SSL. These type of triangles need less information to be interpolated.
    /// </summary>
    internal class SSLTriangleInterpolator : TriangleInterpolator
    {
        public SSLTriangleInterpolator(Triangle t)
            : base(t)
        {
        }

        /// <summary>
        /// Does a perspective-correct, 3-way interpolation of vertex data based on projected point.
        /// Only ProjectedPosition, WorldPosition and Color/ColorTolerance have meaning here.
        /// </summary>
        /// <param name="x">2D Screen-Space pixel X coordinate</param>
        /// <param name="y">2D Screen-Space pixel Y coordinate</param>
        /// <returns>Perspective-correct interpolated Vertex value for this point.</returns>
        override public Vertex GetVertex(double x, double y)
        {
            Vertex interpolation = new Vertex();
            Point3D currRasterPoint = new Point3D(x, y, 0);
            Weights screenWeights = ComputeScreenWeights(currRasterPoint); // This gets us screen weights
            // Use screen weights to find current Z
            Point3D currProjectedPoint = new Point3D(x, y, WeightedSum(vertex1.ProjectedPosition.Z, vertex2.ProjectedPosition.Z, vertex3.ProjectedPosition.Z, screenWeights));
            interpolation.ProjectedPosition = currProjectedPoint;
            // Now that we have currProjectedPoint, we can go ahead and compute other weights
            Weights homogeneousWeights = ComputeHomogeneousWeights(currProjectedPoint);
            // Now we can use the perspectiveCorrectionFactor and the homogeneousWeights to
            // find the perspective-correct weights.
            Weights pcWeights = ComputePerspectiveCorrectWeights(homogeneousWeights);

            // Position ( Eye Space )
            interpolation.Position = Interpolator.WeightedSum(vertex1.Position, vertex2.Position, vertex3.Position, pcWeights);

            // Normal - ignored
            // Texture Coordinates - ignored

            // Color - force V1 since all magic lines are the same
            interpolation.Color = vertex1.Color;

            // Color Error - force black by default
            interpolation.ColorTolerance = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            if (pixelOnEdge)
            {
                // Since numerical precision makes these borderline cases inaccurate,
                // we set the tolerance of this pixel to ignore it.
                interpolation.ColorTolerance = Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF);
            }
            return interpolation;
        }
    }
}
