// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// A triangle in the xy plane (projected polygon) - Used for verification
    /// </summary>
    internal class Triangle
    {
        /// <summary>
        /// Constructor asumes that all vertex data is pre-multiplied
        /// to be in EYE space, and projected.
        /// </summary>
        public Triangle(Vertex v1, Vertex v2, Vertex v3, Matrix3D projectionMatrix)
        {
            vertex1 = v1;
            vertex2 = v2;
            vertex3 = v3;

            vertex1.Project(projectionMatrix);
            vertex2.Project(projectionMatrix);
            vertex3.Project(projectionMatrix);
        }

        /// <summary>
        /// Internal constructor for alternative triangle types that
        /// create their own vertices.
        /// </summary>
        protected Triangle()
        {
        }

        /// <summary>
        /// Returns an interpolator object for calculating 3-way interpolated vertex values.
        /// </summary>
        internal virtual TriangleInterpolator TriangleInterpolator
        {
            get
            {
                return new TriangleInterpolator(this);
            }
        }

        /// <summary>
        /// 2D Projected bounds for this triangle
        /// </summary>
        /// <value></value>
        public Rect Bounds
        {
            get
            {
                double minX = Math.Min(vertex1.ProjectedPosition.X, vertex2.ProjectedPosition.X);
                double minY = Math.Min(vertex1.ProjectedPosition.Y, vertex2.ProjectedPosition.Y);
                double maxX = Math.Max(vertex1.ProjectedPosition.X, vertex2.ProjectedPosition.X);
                double maxY = Math.Max(vertex1.ProjectedPosition.Y, vertex2.ProjectedPosition.Y);

                minX = Math.Min(minX, vertex3.ProjectedPosition.X);
                minY = Math.Min(minY, vertex3.ProjectedPosition.Y);
                maxX = Math.Max(maxX, vertex3.ProjectedPosition.X);
                maxY = Math.Max(maxY, vertex3.ProjectedPosition.Y);

                // Expand bounds to allow for edge tolerance
                minX -= RenderTolerance.PixelToEdgeTolerance;
                minY -= RenderTolerance.PixelToEdgeTolerance;
                maxX += RenderTolerance.PixelToEdgeTolerance;
                maxY += RenderTolerance.PixelToEdgeTolerance;

                // Check for NaN since Avalon doesn't ... (BUG: #1044738)
                if (double.IsNaN(minX) || double.IsNaN(minY) ||
                     double.IsNaN(maxX) || double.IsNaN(maxY))
                {
                    return Rect.Empty;
                }

                return new Rect(new Point(minX, minY), new Point(maxX, maxY));
            }
        }

        /// <summary>
        /// The Vector3D representing the projected triangle's normal
        /// </summary>
        private Vector3D FrontFaceNormal
        {
            get
            {
                Vector3D v21 = vertex2.ProjectedPosition - vertex1.ProjectedPosition;
                Vector3D v32 = vertex3.ProjectedPosition - vertex2.ProjectedPosition;
                return MathEx.CrossProduct(v21, v32);
            }
        }

        /// <summary>
        /// True if projected triangle has CounterClockwise winding
        /// </summary>
        public bool IsFrontFacing
        {
            get
            {
                return FrontFaceNormal.Z < 0.0;
            }
        }

        /// <summary>
        /// True if projected triangle has Clockwise winding
        /// </summary>
        public bool IsBackFacing
        {
            get
            {
                return FrontFaceNormal.Z > 0.0;
            }
        }

        public bool IsDegenerate
        {
            get
            {
                return !(MathEx.NotCloseEnough(vertex1.ProjectedPosition, vertex2.ProjectedPosition) &&
                          MathEx.NotCloseEnough(vertex2.ProjectedPosition, vertex3.ProjectedPosition) &&
                          MathEx.NotCloseEnough(vertex3.ProjectedPosition, vertex1.ProjectedPosition));
            }
        }

        /// <summary>
        /// True if triangle is outside of near/far clipping region
        /// </summary>
        public bool IsClipped
        {
            get
            {
                double minZ = MathEx.Min(
                                    vertex1.ProjectedPosition.Z,
                                    vertex2.ProjectedPosition.Z,
                                    vertex3.ProjectedPosition.Z);
                double maxZ = MathEx.Max(
                                    vertex1.ProjectedPosition.Z,
                                    vertex2.ProjectedPosition.Z,
                                    vertex3.ProjectedPosition.Z);

                return (maxZ < 0) || (minZ > 1);
            }
        }

        /// <summary>
        /// Adds this triangle to the set mesh, at the end.
        /// This requires inverse of some matrices because we operate in EYE space internally.
        /// </summary>
        /// <param name="mesh">Mesh object</param>
        public void SaveToMesh(MeshGeometry3D mesh)
        {
            // use ModelSpace for saving
            mesh.Positions.Add(vertex1.ModelSpacePosition);
            mesh.Positions.Add(vertex2.ModelSpacePosition);
            mesh.Positions.Add(vertex3.ModelSpacePosition);
            mesh.Normals.Add(vertex1.ModelSpaceNormal);
            mesh.Normals.Add(vertex2.ModelSpaceNormal);
            mesh.Normals.Add(vertex3.ModelSpaceNormal);
            mesh.TextureCoordinates.Add(vertex1.TextureCoordinates);
            mesh.TextureCoordinates.Add(vertex2.TextureCoordinates);
            mesh.TextureCoordinates.Add(vertex3.TextureCoordinates);

            // append at the end, non-indexed
            mesh.TriangleIndices.Add(mesh.TriangleIndices.Count);
            mesh.TriangleIndices.Add(mesh.TriangleIndices.Count);
            mesh.TriangleIndices.Add(mesh.TriangleIndices.Count);
        }

        /// <summary>
        /// Returns the Edge representing the intersection of the projected triangle and the near clipping plane.
        /// If no such intersection exists, return null.
        /// </summary>
        internal Edge NearPlaneEdge
        {
            get
            {
                return GetClippingPlaneEdge(vertex1.ProjectedPosition, vertex2.ProjectedPosition, vertex3.ProjectedPosition, 0.0);
            }
        }

        /// <summary>
        /// Returns the Edge representing the intersection of the projected triangle and the far clipping plane.
        /// If no such intersection exists, return null.
        /// </summary>
        internal Edge FarPlaneEdge
        {
            get
            {
                return GetClippingPlaneEdge(vertex1.ProjectedPosition, vertex2.ProjectedPosition, vertex3.ProjectedPosition, 1.0);
            }
        }

        private Edge GetClippingPlaneEdge(Point3D p1, Point3D p2, Point3D p3, double planeDepth)
        {
            double z1 = p1.Z - planeDepth;
            double z2 = p2.Z - planeDepth;
            double z3 = p3.Z - planeDepth;

            bool ss12 = MathEx.SameSign(z1, z2);
            bool ss23 = MathEx.SameSign(z2, z3);
            bool ss31 = MathEx.SameSign(z3, z1);

            // We can determine where the clipping plane intersects the Triangle by performing "same sign" tests
            //  on the z values of the projected vertices
            //
            //      1-------------3
            //       \  /     \  /      The internal lines represent edges created by the clipping plane
            //        \/b     a\/       . if 1 & 2 are on the same side of the plane and 3 is not
            //         \   c   /            - create and return segment a
            //          \-----/         . if 2 & 3 are on the same side of the plane and 1 is not
            //           \   /              - create and return segment b
            //            \ /           . if 3 & 1 are on the same side of the plane and 2 is not
            //             2                - create and return segment c
            //                          . otherwise, the clipping plane does not intersect this triangle
            //                              - return null

            if (ss12 && !ss23)
            {
                // Segment a
                Weights w1 = Interpolator.GetWeightsToXYPlane(p1, p3, planeDepth);
                Weights w2 = Interpolator.GetWeightsToXYPlane(p2, p3, planeDepth);
                Point3D start = Interpolator.WeightedSum(p1, p3, w1);
                Point3D end = Interpolator.WeightedSum(p2, p3, w2);
                return new Edge(start, end);
            }
            if (ss23 && !ss31)
            {
                // Segment b
                Weights w1 = Interpolator.GetWeightsToXYPlane(p1, p2, planeDepth);
                Weights w2 = Interpolator.GetWeightsToXYPlane(p1, p3, planeDepth);
                Point3D start = Interpolator.WeightedSum(p1, p2, w1);
                Point3D end = Interpolator.WeightedSum(p1, p3, w2);
                return new Edge(start, end);
            }
            if (ss31 && !ss12)
            {
                // Segment c
                Weights w1 = Interpolator.GetWeightsToXYPlane(p1, p2, planeDepth);
                Weights w2 = Interpolator.GetWeightsToXYPlane(p2, p3, planeDepth);
                Point3D start = Interpolator.WeightedSum(p1, p2, w1);
                Point3D end = Interpolator.WeightedSum(p2, p3, w2);
                return new Edge(start, end);
            }

            return null;
        }

        /// <summary>
        /// Performs 2D Screen-Space clipping of a point against this triangle.
        /// Also this will set up the pixel-to-edge tolerance flag used later in interpolation.
        /// </summary>
        /// <param name="x">X coordinate with pixel center factored in.</param>
        /// <param name="y">Y coordinate with pixel center factored in.</param>
        /// <returns>TRUE if the point is inside the triangle.</returns>
        public bool Contains(double x, double y)
        {
            pixelOnEdge = false;

            Side side = LineSide(vertex1.ProjectedPosition, vertex2.ProjectedPosition, x, y) &
                        LineSide(vertex2.ProjectedPosition, vertex3.ProjectedPosition, x, y) &
                        LineSide(vertex3.ProjectedPosition, vertex1.ProjectedPosition, x, y);

            switch (side)
            {
                // Front Facing triangle
                case Side.Right:
                    return true;

                // Back Facing triangle
                case Side.Left:
                    return true;

                // The only way that side can be Both for all three lines is if the
                //  vertices of the triangle all lie on the same point.
                // We draw if pixel is on edge so that we write to tolerance buffer
                case Side.Both:
                    return false;

                // Outside, however we need to make provisions for our edge tolerance here.
                case Side.None:
                    return false;

                default:
                    throw new ApplicationException("Invalid Enum Value");
            }
        }

        private enum Side
        {
            None = 0x00,
            Right = 0x01,
            Left = 0x10,
            Both = 0x11
        };

        private Side LineSide(Point3D p1, Point3D p2, double x, double y)
        {
            // If the two points are the same, this is not a line, thus (x,y)
            //  cannot be on either side of it.
            if (MathEx.AreCloseEnough(p1.X, p2.X) && MathEx.AreCloseEnough(p1.Y, p2.Y))
            {
                return Side.None;
            }

            // Account for precision errors along the triangle edges based on 
            // RenderTolerance.PixelToEdgeTolerance value and exit early if we're too close to call.
            double pixelToEdgeTolerance = RenderTolerance.PixelToEdgeTolerance;

            //Increase edge tolerance in Vista nonstandard DPI
            //We *shouldn't* need this at any other time
            //If the ref-renderer is improved to better handle interior edge AA, this can be removed
            if (!RenderTolerance.IsSquare96Dpi&&Const.IsVistaOrNewer)
            {
                pixelToEdgeTolerance *= 3;
            }

            // pixelToEdgeTolerance is only used to generate tolerance map. We can be more precise when we determine
            // if a point is inside a triangle. Use a smaller tolerance for this calculation.
            //TODO: BUGBUG: Pantal - If an extremely small custom tolerance value is applied, this could still cause surprising results.
            // Similarly, if the default PixelToEdge tolerance is increased, troublesome side-effects could arise.
            // A dedicated triangleEdge Tolerance default may be appropriate.
            double triangleEdgeTolerance = Math.Min(pixelToEdgeTolerance, RenderTolerance.DefaultPixelToEdgeTolerance);
            double distanceToLine = MathEx.DistanceFromLine2D(new Point3D(x, y, 0), p1, p2);

            if (Math.Abs(distanceToLine) < pixelToEdgeTolerance)
            {
                // We've just been guaranteed that (x,y) is really close to the line passing through
                //  p1 and p2.
                // This means that if (x,y) is within the bounds of the line *segment* (p1 and p2 are endpoints)
                //  plus tolerance, then (x,y) must be on the line segment (edge).

                Rect bounds = new Rect(new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
                if (MathEx.Inflate(bounds, pixelToEdgeTolerance).Contains(new Point(x, y)))
                {
                    pixelOnEdge = true;
                }

                // We don't know which side we're on because we're too close,
                //  so report that we're on whichever side the caller wants us to be.
                // Any final uncertainty will be decided by the value of "pixelOnEdge."

                if (Math.Abs(distanceToLine) < triangleEdgeTolerance)
                {
                    return Side.Both;
                }
            }

            // Cross product's Z component will be positive if the point is on the left of the line,
            // and negative if the point is on the right. Zero if the point is on the line.
            double crossProductZ = (p1.X - x) * (p2.Y - y) - (p1.Y - y) * (p2.X - x);

            if (MathEx.AreCloseEnough(crossProductZ, 0))
            {
                // For the case where the triangle edge is right over a pixel center
                //  and PixelToEdgeTolerance is too small (e.g. 0), we will test a neighboring pixel.

                // This case counts as on the edge too
                pixelOnEdge = true;

                // The pixel that we test is based on D3D's rendering convention
                //  (this guarantees that we don't render the same edge twice)

                if (!MathEx.AreCloseEnough(p1.Y, p2.Y))
                {
                    return LineSide(p1, p2, x + 1, y);
                }
                else
                {
                    return LineSide(p1, p2, x, y + 1);
                }
            }
            else if (crossProductZ < 0)
            {
                return Side.Right;
            }
            else
            {
                return Side.Left;
            }
        }

        protected bool pixelOnEdge;

        public bool PixelOnEdge
        {
            get { return pixelOnEdge; }
        }

        // These are explicitly public for ease of manipulation
        public Vertex vertex1;
        public Vertex vertex2;
        public Vertex vertex3;

    }

    internal class ScreenSpaceLineTriangle : Triangle
    {
        /// <summary>
        /// Points 1 and 2 are the same (start) until the endpoints
        /// are expanded by thickness in screen space.
        /// </summary>
        /// <param name="start">Start point maps to triangle vertices 1, 2</param>
        /// <param name="end">End point maps to triangle vertex 3</param>
        /// <param name="thickness">SSL Thickness</param>
        /// <param name="color">Line color</param>
        /// <param name="projectionMatrix">used to project the start and end points</param>
        public ScreenSpaceLineTriangle(
                Point3D start,
                Point3D end,
                double thickness,
                Color color,
                Matrix3D projectionMatrix)
            : this(thickness, color)
        {
            vertex1.Position = (Point4D)start;
            vertex2.Position = (Point4D)start;
            vertex3.Position = (Point4D)end;

            vertex1.Project(projectionMatrix);
            vertex2.Project(projectionMatrix);
            vertex3.Project(projectionMatrix);

            // Perform screen space expansion
            ExpandTriangleBaseToThickness(true);
        }

        /// <summary>
        /// Internal constructor used to create vertices based on line points.
        /// Vertices 1, 2 map to start, Vertex3 maps to end.
        /// All expansions in derived classes are guaranteed this order and behavior.
        /// </summary>
        protected ScreenSpaceLineTriangle(double thickness, Color color)
        {
            this.thickness = thickness;

            vertex1 = new Vertex();
            vertex1.Color = color;

            vertex2 = new Vertex();
            vertex2.Color = color;

            vertex3 = new Vertex();
            vertex3.Color = color;
        }

        internal override TriangleInterpolator TriangleInterpolator
        {
            get
            {
                return new SSLTriangleInterpolator(this);
            }
        }

        protected void ExpandTriangleBaseToThickness(bool doDpiScaling)
        {
            // They had the same math performed on them.  They'd better be the same.
            System.Diagnostics.Debug.Assert(MathEx.Equals(vertex1.ProjectedPosition, vertex2.ProjectedPosition));

            // The line can be treated as 2D now that it has been projected onto the screen
            // We can assume that vertex1.ProjectedPosition and vertex2.ProjectedPosition are the same
            double dy = vertex1.ProjectedPosition.Y - vertex3.ProjectedPosition.Y;
            double dx = vertex1.ProjectedPosition.X - vertex3.ProjectedPosition.X;

            // Move the points along the lines perpendicular to the ScreenSpaceLine passing through each endpoint
            Vector3D invSlope = MathEx.Normalize(new Vector3D(-dy, dx, 0));

            // Move each point half of the thickness from the ScreenSpaceLine's center
            invSlope *= 0.5 * thickness;

            // Avalon SSL's should be Dpi scaled, and SilhouetteEdges should not
            if (doDpiScaling)
            {
                invSlope.X = MathEx.ConvertToAbsolutePixelsX(invSlope.X);
                invSlope.Y = MathEx.ConvertToAbsolutePixelsY(invSlope.Y);
            }

            // Move the two vertices at the same endpoint in opposite directions
            // The third vertex goes in the same direction as vertex1.ProjectedPosition
            //  (but it could just as easily go with vertex2.ProjectedPosition)
            //
            // IF THIS CHANGES, YOU MUST UPDATE SSLProjectedGeometry.cs whose SilhouetteEdgeFinder depends on this behavior
            //
            // Goes from this:
            //
            //  12------------------3
            //
            // To this:
            //
            //  1-------------------3
            //  |    ____....''''
            //  2''''
            //
            vertex1.ProjectedPosition += invSlope;
            vertex2.ProjectedPosition -= invSlope;
            vertex3.ProjectedPosition += invSlope;
        }

        internal double thickness;
    }

    internal class SilhouetteEdgeTriangle : ScreenSpaceLineTriangle
    {
        public SilhouetteEdgeTriangle(Point3D projectedStart, Point3D projectedEnd, double edgeDetectionTolerance)
            : base(2.0 * Const.RootTwo * edgeDetectionTolerance, Colors.White)
        {
            // Note on edge detection tolerance:
            // The old image-space behavior had 1 be a 2 pixel border around the edge.
            // This version has EdgeDetectionTolerance=1 be the equivalent on the worse case scenario,
            //   so the line is of Thickness 2 * Sqrt(2) * tolerance.

            // We already have the projected positions,
            // pass them down in the same order as the base
            vertex1.ProjectedPosition = projectedStart;
            vertex2.ProjectedPosition = projectedStart;
            vertex3.ProjectedPosition = projectedEnd;

            // These triangles are already projected, expand the bases here.
            ExtendTriangleLengthByThickness();
            ExpandTriangleBaseToThickness(false);
        }

        protected void ExtendTriangleLengthByThickness()
        {
            // Extend the line along the edge first

            // The line can be treated as 2D now that it has been projected onto the screen
            // We can assume that vertex1.ProjectedPosition and vertex2.ProjectedPosition are the same
            double dy = vertex1.ProjectedPosition.Y - vertex3.ProjectedPosition.Y;
            double dx = vertex1.ProjectedPosition.X - vertex3.ProjectedPosition.X;

            // Move the points along the lines perpendicular to the ScreenSpaceLine
            // passing through each endpoint
            Vector3D slope = MathEx.Normalize(new Vector3D(dx, dy, 0));

            // Move each point half of the thickness out from the endpoints
            // Do NOT correct for DPI because we want actual pixels.
            slope *= 0.5 * thickness;

            // Stretch the line along the endpoints
            vertex1.ProjectedPosition += slope;
            vertex2.ProjectedPosition += slope;
            vertex3.ProjectedPosition -= slope;
        }
    }
}
