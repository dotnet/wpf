// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

using Microsoft.Internal;
using System.Runtime.CompilerServices;
using MS.Internal.ReachFramework;

using System.Security;
using System.Windows.Xps.Serialization;
using MS.Utility;

using BuildInfo = MS.Internal.ReachFramework.BuildInfo;

[assembly: InternalsVisibleTo(       "System.Printing, PublicKey=" + BuildInfo.WCP_PUBLIC_KEY_STRING)]

// This code is debug only until we decide to go all the way with enforcements.
#if ENFORCEMENT
#endif

namespace Microsoft.Internal.AlphaFlattener
{
#if DEBUG
	internal static class StopWatch
	{
		static double   s_total; // = 0;
		static DateTime s_startTime;
		static int      s_count; // = 0;

		internal static void Start()
		{
			s_startTime = DateTime.Now;
		}

		internal static void Stop()
		{
			TimeSpan elapsed = DateTime.Now - s_startTime;

			s_total += elapsed.TotalSeconds;
			s_count++;

            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine("{0} {1} {2}", s_count, elapsed.TotalSeconds, s_total);
            }
        }
	}
#endif

	internal static class Utility
    {
        #region Constants

        private const double OpaqueEnough      = 250.0 / 255; // 0.98
        private const double AlmostTransparent = 5.0 / 255; // 0.02

        // Espilon for detecting two doubles as the same. It's a pretty large epsilon due to
        // high error when dealing with PathGeometry.
        private const double Epsilon = 0.00002;

        /// <summary>
        /// Minimum geometry width and height in world space for it to be considered visible.
        /// </summary>
        /// <remarks>
        /// Currently defined as 1x1 rectangle at 960 DPI.
        /// </remarks>
        private const double GeometryMinimumDimension = 1 * (96.0 / 960.0);

        /// <summary>
        /// Numerical limits from the XPS specification.
        /// </summary>
        private const double XpsMaxDouble = 1e38;
        private const double XpsMinDouble = -1e38;

        /// <summary>
        /// Drawing cost of having transparency.
        /// </summary>
        public const double TransparencyCostFactor = 2.5;

        #endregion

        #region Math and Transform

        /// <summary>
        /// Apply transformation to Rect
        /// </summary>
        static public Rect TransformRect(Rect r, Matrix t)
        {
            if (t.IsIdentity)
            {
                return r;
            }

            r.Transform(t);
            return r;
        }

        /// <summary>
        /// IsOne - Returns whether or not the double is "close" to 1.
        /// </summary>
        /// <param name="value"> The double to compare to 1. </param>
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < Epsilon;
        }

        /// <summary>
        /// IsZero - Returns whether or not the double is "close" to 0.
        /// </summary>
        /// <param name="value"> The double to compare to 0. </param>
        public static bool IsZero(double value)
        {
            return Math.Abs(value) < Epsilon;
        }

        static public bool AreClose(double v1, double v2)
        {
            return IsZero(v1 - v2);
        }

        static public bool AreClose(Point p1, Point p2)
        {
            return IsZero(p1.X - p2.X) && IsZero(p1.Y - p2.Y);
        }

        static public bool AreClose(Vector v1, Vector v2)
        {
            return IsZero(v1.X - v2.X) && IsZero(v2.Y - v2.Y);
        }

        static public bool AreClose(Size s1, Size s2)
        {
            return IsZero(s1.Width - s2.Width) && IsZero(s1.Height - s2.Height);
        }

        static public bool AreClose(Rect r1, Rect r2)
        {
            return AreClose(r1.TopLeft, r2.TopLeft) && AreClose(r1.BottomRight, r2.BottomRight);
        }

        static public bool IsMultipleOf(double v1, double v2)
        {
            if (IsZero(v2))
            {
                return IsZero(v1);
            }

            double scale = v1 / v2;

            double s = Math.Round(scale);

            return (s >= 1) && IsZero(scale - s);
        }

        static public bool IsScaleTranslate(Matrix transform)
        {
            return IsZero(transform.M12) && IsZero(transform.M21);
        }

        // x' = m11 * x + m21 * y + dx
        // y' = m12 * x + m22 * y + dy
        // When m11^2 + m12^2 = m21^2 + m22^2 and m11 * m21 + m12 * m22 = 0
        // Distance between any two points will be scaled by a constant
        static public bool HasUniformScale(Matrix mat, out double scale)
        {
            scale = 1;

            double A = mat.M11 * mat.M11 + mat.M12 * mat.M12;
            double B = mat.M21 * mat.M21 + mat.M22 * mat.M22;

            if (IsZero(A - B))
            {
                double C = mat.M11 * mat.M21 + mat.M12 * mat.M22;

                scale = Math.Sqrt(A);

                return IsZero(C);
            }

            return false;
        }

        // Euclidean distance: optimized for common cases where either x or y is zero.
        public static double Hypotenuse(double x, double y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);

            if (IsZero(x))
            {
                return y;
            }
            else if (IsZero(y))
            {
                return x;
            }
            else
            {
                if (y < x)
                {
                    double temp = x;
                    x = y;
                    y = temp;
                }

                double r = x / y;

                return y * Math.Sqrt(r * r + 1.0);
            }
        }

        public static double GetScaleX(Matrix matrix)
        {
            return Hypotenuse(matrix.M11, matrix.M21);
        }

        public static double GetScaleY(Matrix matrix)
        {
            return Hypotenuse(matrix.M12, matrix.M22);
        }

        public static double GetScale(Matrix trans)
        {
            return Math.Max(GetScaleX(trans), GetScaleY(trans));
        }

        public static bool IsIdentity(Matrix mat)
        {
            if (mat.IsIdentity)
            {
                return true;
            }
            else
            {
                return IsOne(mat.M11) && IsZero(mat.M12) &&
                       IsZero(mat.M21) && IsOne(mat.M22) &&
                       IsZero(mat.OffsetX) && IsZero(mat.OffsetY);
            }
        }

        // Check if transform is null, identity or close enough to identity
        public static bool IsIdentity(Transform transform)
        {
            if (transform == null)
            {
                return true;
            }
            else
            {
                return IsIdentity(transform.Value);
            }
        }

        /// <summary>
        /// Merge transform with relative transform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="relative"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static Matrix MergeTransform(Transform trans, Transform relative, Rect bounds)
        {
            Matrix mat = Matrix.Identity;

            if (!IsIdentity(relative))
            {
                // Calculate absolute derivation of the relative tranform
                // Refer to mil\core\resources\BrushTypeUtils.cpp: CBrushTypeUtils::ConvertRelativeTransformToAbsolute
                mat = relative.Value;

                //      MILRectF relativeBounds = {0.0, 0.0, 1.0, 1.0};
                //      pResultTransform->InferAffineMatrix(relativeBounds, pBoundingBox);
                //      pResultTransform->Multiply(*pmatRelative);
                //      relativeToAbsolute.InferAffineMatrix(pBoundingBox, relativeBounds);
                //      pResultTransform->Multiply(relativeToAbsolute);

                mat.OffsetX = mat.OffsetX * bounds.Width - mat.M11 * bounds.X - mat.M21 * bounds.Y * bounds.Width / bounds.Height + bounds.X;
                mat.OffsetY = mat.OffsetY * bounds.Height - mat.M12 * bounds.X * bounds.Height / bounds.Width - mat.M22 * bounds.Y + bounds.Y;

                mat.M12 *= bounds.Height / bounds.Width;
                mat.M21 *= bounds.Width / bounds.Height;
            }

            if (!IsIdentity(trans))
            {
                mat *= trans.Value;
            }

            return mat;
        }

        public static Point MapPoint(Rect bounds, Point p)
        {
            return new Point(bounds.Left + p.X * bounds.Width,
                             bounds.Top + p.Y * bounds.Height);
        }

        public static Transform MultiplyTransform(Transform trans1, Transform trans2)
        {
            if ((trans1 == null) || trans1.Value.IsIdentity)
            {
                return trans2;
            }

            if ((trans2 == null) || trans2.Value.IsIdentity)
            {
                return trans1;
            }

            Matrix mat = trans1.Value;

            mat.Append(trans2.Value);

            return new MatrixTransform(mat);
        }

        /// <summary>
        /// Creates transformation mapping from first rectangle to the second rectangle.
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        public static Matrix CreateMappingTransform(Rect r1, Rect r2)
        {
            Matrix transform = Matrix.CreateTranslation(-r1.X, -r1.Y);

            transform.Scale(r2.Width / r1.Width, r2.Height / r1.Height);
            transform.Translate(r2.X, r2.Y);

            return transform;
        }

        /// <summary>
        /// Creates transformation mapping from first rectangle to rectangle at origin with specified dimensions.
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Matrix CreateMappingTransform(Rect r1, double width, double height)
        {
            Matrix transform = Matrix.CreateTranslation(-r1.X, -r1.Y);

            transform.Scale(width / r1.Width, height / r1.Height);

            return transform;
        }

        #endregion

        #region Geometry

        /// <summary>
        /// Apply transformation to Geometry
        /// </summary>
        static public Geometry TransformGeometry(Geometry g, Matrix t)
        {
            if (g == null)
            {
                return null;
            }

            if (t.IsIdentity)
            {
                return g;
            }

            Geometry newg = g.CloneCurrentValue();

            newg.Transform = MultiplyTransform(newg.Transform, new MatrixTransform(t));

            return newg;
        }

        /// <summary>
        /// Apply transformation to Geometry
        /// </summary>
        static public Geometry TransformGeometry(Geometry g, Transform t)
        {
            if (g == null)
            {
                return null;
            }

            if (t.Value.IsIdentity)
            {
                return g;
            }

            Geometry newg = g.CloneCurrentValue();

            newg.Transform = MultiplyTransform(newg.Transform, t);

            return newg;
        }

        static public Geometry InverseTransformGeometry(Geometry g, Matrix mat)
        {
            if ((g == null) || mat.IsIdentity)
            {
                return g;
            }

            mat.Invert();

            return TransformGeometry(g, mat);
        }

        /// <summary>
        /// Checks if a Geometry can be considered empty after transformation.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="mat">Transformation hint to world space, may be identity</param>
        /// <remarks>
        /// mat is used to transform geometry bounds to world space to test for
        /// non-visible geometry.
        /// </remarks>
        static public bool IsEmpty(Geometry shape, Matrix mat)
        {
            if (shape == null)
            {
                return true;
            }

            Rect bounds = shape.Bounds;

            // If bounding rectangle is empty, the shape is empty
            if (bounds.IsEmpty)
            {
                return true;
            }

            if (!mat.IsIdentity)
            {
                bounds.Transform(mat);

                if (bounds.Width < GeometryMinimumDimension || bounds.Height < GeometryMinimumDimension)
                {
                    // geometry bounds in world space is too small, treat as empty
                    return true;
                }
            }

            return false;
        }

        [FriendAccessAllowed]
        static public PathGeometry GetAsPathGeometry(Geometry geo)
        {
            PathGeometry pg = geo as PathGeometry;

            if (pg == null)
            {
                pg = PathGeometry.CreateFromGeometry(geo);
            }

            return pg;
        }

        [FriendAccessAllowed]
        static public bool IsRectangle(Geometry geometry)
        {
            if (geometry.Transform != null && !IsScaleTranslate(geometry.Transform.Value))
            {
                // assume the transformation distorts the geometry, thus it can't be rectangle
                return false;
            }

            bool rect = geometry is RectangleGeometry;

            if (!rect)
            {
                StreamGeometry streamGeometry = geometry as StreamGeometry;

                if (streamGeometry != null)
                {
                    rect = IsRectangle(streamGeometry);
                }
            }

            if (!rect)
            {
                PathGeometry pathGeometry = geometry as PathGeometry;

                if (pathGeometry != null)
                {
                    rect = IsRectangle(pathGeometry);
                }
            }

            return rect;
        }

        // Checks if StreamGeometry is a rectangle
        private static bool IsRectangle(StreamGeometry geometry)
        {
            int pointCount;
            bool isRectangle;
            bool isLineSegment;

            GeometryAnalyzer.Analyze(
                geometry.GetPathGeometryData(),
                geometry.Bounds,
                out pointCount,
                out isRectangle,
                out isLineSegment
                );

            return isRectangle;
        }

        // Check if a PathGeometry is a normal rectangle
        private static bool IsRectangle(PathGeometry geometry)
        {
            if ((geometry == null) || (geometry.Figures.Count != 1))
            {
                return false;
            }

            PathFigure figure = geometry.Figures[0];

            if ((figure != null) && figure.IsClosed)
            {
                Point p = figure.StartPoint;

                int n = 0;

                for (int s = 0; s < figure.Segments.Count; s++)
                {
                    PathSegment segment = figure.Segments[s];

                    if (segment != null)
                    {
                        LineSegment seg = segment as LineSegment;
                        PolyLineSegment pseg = null;

                        int count = 1;

                        if (seg == null)
                        {
                            pseg = segment as PolyLineSegment;

                            if ((pseg == null) || (pseg.Points == null) || (pseg.Points.Count != 3))
                            {
                                return false;
                            }

                            count = 3;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            Point q;

                            if (seg != null)
                            {
                                q = seg.Point;
                            }
                            else
                            {
                                q = pseg.Points[i];
                            }

                            if (!IsOnRectangle(figure.StartPoint, p, q, n))
                            {
                                return false;
                            }

                            p = q;
                            n++;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return n >= 3; // 3 or 4 points
            }

            return false;
        }

        /// <summary>
        /// Non-exhaustive check if geometry is a single line segment.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static bool IsLineSegment(Geometry geometry)
        {
            StreamGeometry streamGeometry = geometry as StreamGeometry;

            if (streamGeometry != null)
            {
                int estimatedPointCount;    // unused
                bool isRectangle;           // unused
                bool isLineSegment;

                GeometryAnalyzer.Analyze(
                    streamGeometry.GetPathGeometryData(),
                    /*checkRectangular=*/null,
                    out estimatedPointCount,
                    out isRectangle,
                    out isLineSegment
                    );

                return isLineSegment;
            }

            PathGeometry pathGeometry = geometry as PathGeometry;

            if (pathGeometry != null &&
                pathGeometry.Figures != null &&
                pathGeometry.Figures.Count == 1)
            {
                PathFigure figure = pathGeometry.Figures[0];

                if (figure.Segments != null &&
                    figure.Segments.Count == 1 &&
                    figure.Segments[0] is LineSegment)
                {
                    return true;
                }
            }

            return false;
        }


        static public double GetGeometryCost(Geometry g)
        {
            StreamGeometry sg = g as StreamGeometry;

            if (sg != null)
            {
                int  pointCount;
                bool isRectangle;
                bool isLineSegment;

                GeometryAnalyzer.Analyze(
                    sg.GetPathGeometryData(),
                    sg.Bounds,
                    out pointCount,
                    out isRectangle,
                    out isLineSegment
                );

                return pointCount;
            }

            PathGeometry pg = g as PathGeometry;

            if (pg != null)
            {
                return GetPathPointCount(pg);
            }

            GeometryGroup gg = g as GeometryGroup;

            if (gg != null)
            {
                double sum = 0;

                if (gg.Children != null)
                {
                    foreach (Geometry c in gg.Children)
                    {
                        sum += GetGeometryCost(c);
                    }
                }

                return sum;
            }

            if (g is RectangleGeometry)
            {
                return 4;
            }

            return 10000;
        }


        /// <summary>
        /// Check if the first Geometry completely covers the second one.
        /// It's only used for optimization, so it's okay to return false anytime.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns>True if Intersect(one, two) = two</returns>
        static public bool FullyCovers(Geometry one, Geometry two)
        {
            Rect bounds1 = one.Bounds;
            Rect bounds2 = two.Bounds;

            // Check if the bounds of one covers the bounds of two
            if (bounds1.Contains(bounds2))
            {
                if (IsRectangle(one))
                {
                    return true;
                }

                double cost1 = GetGeometryCost(one);
                double cost2 = GetGeometryCost(two);

                // Avoid expensive calls to convert to PathGeometry and calling ContainsWithDetail
                if (cost1 * cost2 < 10000) // 100 x 100
                {
                    IntersectionDetail detail = one.FillContainsWithDetail(two);

                    return (detail == IntersectionDetail.FullyContains);
                }
                else if (cost1 < 500)
                {
                    IntersectionDetail detail = one.FillContainsWithDetail(new RectangleGeometry(bounds2));

                    return (detail == IntersectionDetail.FullyContains);
                }
            }

            return false;
        }

        static public bool Covers(Geometry one, Geometry two)
        {
            Rect bounds1 = one.Bounds;
            Rect bounds2 = two.Bounds;

            if (bounds1.Contains(bounds2))
            {
                if (one is RectangleGeometry && one.Transform.Value.IsIdentity)
                {
                    return true;
                }
            }

            IntersectionDetail detail = one.FillContainsWithDetail(two);

            return (detail == IntersectionDetail.FullyContains);
        }

        /// <summary>
        /// Find intersection of two Geometries
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="mat">Transformation of resulting intersection geometry to world space for optimization purposes;
        /// may be null.</param>
        /// <param name="empty"></param>
        /// <returns></returns>
        /// <remarks>
        /// mat is used to estimate world-space size of the intersection, and return empty if
        /// intersection result is too small to be visible.
        /// </remarks>
        static public Geometry Intersect(Geometry one, Geometry two, Matrix mat, out bool empty)
        {
            empty = false;

            if (one == null) // null is whole
            {
                return two;
            }

            if (two == null)
            {
                return one;
            }

            if (FullyCovers(one, two))
            {
                return two;
            }

            if (FullyCovers(two, one))
            {
                return one;
            }

            if (one.Bounds.IntersectsWith(two.Bounds))
            {
                one = Combine(one, two, GeometryCombineMode.Intersect, mat);
            }
            else
            {
                one = null;
            }

            empty = one == null;

            return one;
        }

        /// <summary>
        /// Excludes second geometry from the first.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="mat">Transformation of resulting intersection geometry to world space for optimization purposes;
        /// may be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// mat is used to estimate world-space size of the intersection, and return empty if
        /// intersection result is too small to be visible.
        /// </remarks>
        static public Geometry Exclude(Geometry one, Geometry two, Matrix mat)
        {
            if ((one == null) || (two == null))
            {
                return one;
            }

            return Combine(one, two, GeometryCombineMode.Exclude, mat);
        }

        /// <summary>
        /// Check if a clip Geometry is disjoint with a bounding box
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool Disjoint(Geometry clip, Rect rect)
        {
            // Null clip means no clipping
            if (clip == null)
            {
                return false;
            }

            Rect cBounds = clip.Bounds;

            return (cBounds.Left > rect.Right) ||
                   (cBounds.Right < rect.Left) ||
                   (cBounds.Top > rect.Bottom) ||
                   (cBounds.Bottom < rect.Top);
        }

        /// <summary>
        /// Gets upper bound to number of points in geometry data.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static int GetGeometryPointCount(Geometry geometry)
        {
            int estimatedPoints;
            bool usePathGeometry = false;

            if (geometry is PathGeometry ||
                geometry is GeometryGroup ||
                geometry is CombinedGeometry)
            {
                //
                // For GeometryGroup and CombinedGeometry, Geometry.GetPathGeometryData has hidden
                // conversion to PathGeometry followed by serialization to PathGeometryData.
                //
                // We therefore simply convert to PathGeometry and walk it to get point count.
                //
                usePathGeometry = true;
            }

            if (usePathGeometry)
            {
                PathGeometry pathGeometry = GetAsPathGeometry(geometry);

                estimatedPoints = GetPathPointCount(pathGeometry);
            }
            else
            {
                Geometry.PathGeometryData data = geometry.GetPathGeometryData();

                estimatedPoints = GetGeometryDataPointCount(data);
            }

            return estimatedPoints;
        }

        [FriendAccessAllowed]
        public static int GetGeometryDataPointCount(Geometry.PathGeometryData geometryData)
        {
            int pointCount;

            GeometryAnalyzer.Analyze(geometryData, out pointCount);

            return pointCount;
        }

        [FriendAccessAllowed]
        public static int GetPathPointCount(PathGeometry geometry)
        {
            int size = 0;

            for (int i = 0; i < geometry.Figures.Count; i++)
            {
                PathFigure figure = geometry.Figures[i];

                if (figure != null)
                {
                    size += GetPathPointCount(figure);
                }
            }

            return size;
        }

        [FriendAccessAllowed]
        public static int GetPathPointCount(PathFigure figure)
        {
            int size = 2;     // For the startpoint and endpoint

            for (int s = 0; s < figure.Segments.Count; s++)
            {
                PathSegment segment = figure.Segments[s];

                if (segment != null)
                {
                    size += GetPathPointCount(segment);
                }
            }

            return size;
        }

        [FriendAccessAllowed]
        public static int GetPathPointCount(PathSegment segment)
        {
            Type typ = segment.GetType();
            int size;

            if (typ == typeof(BezierSegment))
            {
                size = 3;
            }
            else if (typ == typeof(LineSegment))
            {
                size = 1;
            }
            else if (typ == typeof(QuadraticBezierSegment))
            {
                size = 3;
            }
            else if (typ == typeof(PolyLineSegment))
            {
                PolyLineSegment seg = (PolyLineSegment)segment;

                size = seg.Points.Count;
            }
            else if (typ == typeof(PolyQuadraticBezierSegment))
            {
                PolyQuadraticBezierSegment seg = (PolyQuadraticBezierSegment)segment;

                size = (seg.Points.Count + 1) / 2 * 3;
            }
            else if (typ == typeof(PolyBezierSegment))
            {
                PolyBezierSegment seg = (PolyBezierSegment)segment;

                size = (seg.Points.Count + 2) / 3 * 3;
            }
            else if (typ == typeof(ArcSegment))
            {
                // An arc can be converted to a maxiumum of 4 Bezier segments, Check ArcToBezier in DrawingContextFlattener.cs
                size = 4 * 3;
            }
            else
            {
                Debug.Assert(false, "Unsupported PathSegment");
                size = 0;
            }

            return size;
        }

        private const double Tolerance_960_dpi = 0.1;

        static private Geometry Combine(Geometry one, Geometry two, GeometryCombineMode mode, Matrix mat)
        {
#if DEBUG
            StopWatch.Start();
#endif

            Geometry rslt = System.Windows.Media.Geometry.Combine(one, two, mode,
                                Transform.Identity,
                                Tolerance_960_dpi, ToleranceType.Absolute);

#if DEBUG

            if (Configuration.Verbose >= 2)
            {
                if (IsRectangle(GetAsPathGeometry(one)) && IsRectangle(GetAsPathGeometry(two)))
                {
                    Console.WriteLine("Combine({0})", mode);
                }
            }

			StopWatch.Stop();
#endif

            if (IsEmpty(rslt, mat))
            {
                return null;
            }
            else
            {
                return rslt;
            }
        }

        static private bool IsOnRectangle(Point start, Point p, Point q, int n)
        {
            switch (n)
            {
                case 0:
                    if (!AreClose(q.Y, p.Y) || (q.X < p.X))
                    {
                        return false;
                    }
                    break;

                case 1:
                    if (!AreClose(q.X, p.X) || (q.Y < p.Y))
                    {
                        return false;
                    }
                    break;

                case 2:
                    if (!AreClose(q.Y, p.Y) || !AreClose(q.X, start.X))
                    {
                        return false;
                    }
                    break;

                case 3:
                    if (!AreClose(q.Y, start.Y) || !AreClose(q.X, start.X))
                    {
                        return false;
                    }
                    break;

                default:
                    return false;
            }

            return true;
        }

        #endregion

        #region Color

        /// <summary>
        /// Get opacity to be in [0..1], default = 1
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        public static double GetOpacity(Brush brush)
        {
            if (brush == null)
            {
                return 0;   // transparent
            }

            double opacity = brush.Opacity;

            if (opacity > 1)
            {
                return 1;
            }
            else if  (Double.IsNaN(opacity) || (opacity < 0))
            {
                return 0;
            }
            else
            {
                return opacity;
            }
        }


        /// <summary>
        /// Check if opacity is high enough to be considered totally opaque
        /// </summary>
        public static bool IsOpaque(double opacity)
        {
            return Utility.NormalizeOpacity(opacity) > OpaqueEnough;
        }


        /// <summary>
        /// Check if opacity is low enough to be considered totally transparent
        /// </summary>
        public static bool IsTransparent(double opacity)
        {
            return Utility.NormalizeOpacity(opacity) < AlmostTransparent;
        }

        /// <summary>
        /// Check if a brush is opacity
        /// </summary>
        /// <param name="brush"></param>
        /// <returns>True if brush is known to be opaque. It's okay to return false</returns>
        public static bool IsBrushOpaque(Brush brush)
        {
            if (IsOpaque(brush.Opacity))
            {
                SolidColorBrush sb = brush as SolidColorBrush;

                if (sb != null)
                {
                    return Utility.IsOpaque(sb.Color.ScA);
                }

                // More common brushes can be added later
            }

            return false;
        }

        /// <summary>
        /// Converts a floating-point opacity value to a byte value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte OpacityToByte(double value)
        {
            return (byte)(NormalizeOpacity(value) * 255);
        }

        /// <summary>
        /// Converts a floating-point color channel value to a byte value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte ColorToByte(float value)
        {
            return (byte)(NormalizeColorChannel(value) * 255);
        }

        /// <summary>
        /// Calculate the blended color of two colors, the color which can achieve
        /// the same result as drawing two colors seperately
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static public Color BlendColor(Color x, Color y)
        {
            if (Configuration.ForceAlphaOpaque)
            {
                y.ScA = 1;

                return y;
            }

            Byte a = x.A;
            Byte b = y.A;
            int c = a * (255 - b);
            int d = (a + b) * 255 - a * b;

            if (d < 255) // 0.004
            {
                return Color.FromArgb(0, 255, 255, 255); // transparent white
            }
            else
            {
                Byte red   = (Byte)((x.R * c + y.R * b * 255) / d);
                Byte green = (Byte)((x.G * c + y.G * b * 255) / d);
                Byte blue  = (Byte)((x.B * c + y.B * b * 255) / d);
                Color ret  = Color.FromArgb((Byte)(d / 255), red, green, blue);

                return ret;
            }
        }

        /// <summary>
        /// Multiply a color's alpha channel by an opacity value
        /// </summary>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public static Color Scale(Color color, double opacity)
        {
            if (IsOpaque(opacity))
            {
                return color;
            }

            // Do not return transparent white if opacity is transparent.
            // We still need to preserve color channels for gradient stop colors.

            color = Utility.NormalizeColor(color);
            opacity = Utility.NormalizeOpacity(opacity);

            color.ScA = (float)(color.ScA * opacity);

            return color;
        }

        #endregion

        #region Image

        // val could be larger than 255 * 255 because of super lumbinance
        static Byte Div255(int val)
        {
            if (val > 255 * 255)
            {
                return 255;
            }
            else if (val < 128)
            {
                return 0;
            }
            else
            {
                return (Byte) (val / 255);
            }
        }

        /// <summary>
        /// Blend an array of PBGRA pixels with a given Color rendering under it
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="count"></param>
        /// <param name="colorX"></param>
        /// <param name="opacity"></param>
        /// <param name="opacityOnly">Only use the alpha channel in the image</param>
        static public void BlendOverColor(byte[] pixels, int count, Color colorX, double opacity, bool opacityOnly)
        {
/*          if (Configuration.ForceAlphaOpaque)
            {
                for (int q = 3; q < count * 4; q += 4)
                {
                    pixels[q] = 255; // Force opaque
                }

                return;
            }
*/

            Byte xA = colorX.A;
            Byte xR = colorX.R;
            Byte xG = colorX.G;
            Byte xB = colorX.B;

            int p = 0;

            int op = OpacityToByte(opacity);

            while (count > 0)
            {
                int b = pixels[p + 3] * op / 255;        // pixel.Opacity * opacity

                if (opacityOnly)
                {
                    Byte pa       = (Byte) (b  * xA / 255);   // pixel.Opacity * opacity * colorX.A;

                    pixels[p]     = (Byte) (xB * pa / 255);
                    pixels[p + 1] = (Byte) (xG * pa / 255);
                    pixels[p + 2] = (Byte) (xR * pa / 255);
                    pixels[p + 3] = pa;
                }
                else
                {
                    int c = xA * (255 - b) / 255;             // colorX.A * (1  - pixel.Opacity * opacity)

                    pixels[p]     = Div255(xB * c + pixels[p    ] * op);
                    pixels[p + 1] = Div255(xG * c + pixels[p + 1] * op);
                    pixels[p + 2] = Div255(xR * c + pixels[p + 2] * op);
                    pixels[p + 3] = Div255((xA + b) * 255 - xA * b);
                }

                p += 4;
                count --;
            }
        }

        /// <summary>
        /// Blend an array of PBGRA pixels with a given Color rendering over it
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="count"></param>
        /// <param name="colorY"></param>
        /// <param name="opacity"></param>
        /// <param name="opacityOnly">Only use the alpha channel in the image</param>
        static public void BlendUnderColor(byte[] pixels, int count, Color colorY, double opacity, bool opacityOnly)
        {
            Byte b  = colorY.A;
            Byte yR = colorY.R;
            Byte yG = colorY.G;
            Byte yB = colorY.B;

            int yRb = colorY.R * b;
            int yGb = colorY.G * b;
            int yBb = colorY.B * b;

            int p = 0;

            int op = OpacityToByte(opacity);

            int op1_b = op * (255 -b ) / 255;

            while (count > 0)
            {
                int a = pixels[p + 3] * op / 255;

                if (opacityOnly)
                {
                    Byte pa       = (Byte)(b * a / 255);

                    pixels[p]     = (Byte)(yB * pa / 255);
                    pixels[p + 1] = (Byte)(yG * pa / 255);
                    pixels[p + 2] = (Byte)(yR * pa / 255);
                    pixels[p + 3] = pa;
                }
                else
                {
                    pixels[p    ] = Div255(pixels[p]     * op1_b + yBb);
                    pixels[p + 1] = Div255(pixels[p + 1] * op1_b + yGb);
                    pixels[p + 2] = Div255(pixels[p + 2] * op1_b + yRb);
                    pixels[p + 3] = Div255((a + b) * 255 - a * b);
                }

                p += 4;
                count --;
            }
        }

        /// <summary>
        /// Blend two PBGRA buffer into one PBGRA buffer
        /// </summary>
        /// <param name="pixelsA"></param>
        /// <param name="opacityOnlyA"></param>
        /// <param name="pixelsB"></param>
        /// <param name="opacityOnlyB"></param>
        /// <param name="count"></param>
        /// <param name="pixelsC">Output pixel array</param>
        static public void BlendPixels(byte[] pixelsA, bool opacityOnlyA, byte[] pixelsB, bool opacityOnlyB, int count, byte[] pixelsC)
        {
            int p = 0;

            while (count > 0)
            {
                count --;

                Byte a = pixelsA[p + 3]; // alpha for A
                Byte b = pixelsB[p + 3]; // alpha for B

                if (opacityOnlyA)
                {
                    pixelsC[p] = (Byte)(pixelsB[p] * a / 255); p++;
                    pixelsC[p] = (Byte)(pixelsB[p] * a / 255); p++;
                    pixelsC[p] = (Byte)(pixelsB[p] * a / 255); p++;
                    pixelsC[p] = (Byte)(a * b / 255);
                }
                else if (opacityOnlyB)
                {
                    pixelsC[p] = (Byte)(pixelsA[p] * b / 255); p++;
                    pixelsC[p] = (Byte)(pixelsA[p] * b / 255); p++;
                    pixelsC[p] = (Byte)(pixelsA[p] * b / 255); p++;
                    pixelsC[p] = (Byte)(a * b / 255);
                }
                else
                {
                    pixelsC[p] = Div255(pixelsA[p] * (255 - b) + pixelsB[p] * 255); p++;
                    pixelsC[p] = Div255(pixelsA[p] * (255 - b) + pixelsB[p] * 255); p++;
                    pixelsC[p] = Div255(pixelsA[p] * (255 - b) + pixelsB[p] * 255); p++;

                    pixelsC[p] = Div255((a + b) * 255 - a * b);
                }

                p ++;
            }
        }

        /// <summary>
        /// Clips pixels to specified rectangle.
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x0">Left position of clip rectangle.</param>
        /// <param name="y0"></param>
        /// <param name="clipWidth"></param>
        /// <param name="clipHeight"></param>
        /// <returns>Returns array of clipped pixels.</returns>
        static public byte[] ClipPixels(byte[] pixels, int width, int height, int x0, int y0, int clipWidth, int clipHeight)
        {
            Debug.Assert(
                (x0 >= 0) &&
                (y0 >= 0) &&
                ((x0 + clipWidth) <= width) &&
                ((y0 + clipHeight) <= height)
                );

            if (x0 == 0 && y0 == 0 && clipWidth == width && clipHeight == height)
            {
                // no clipping
                return pixels;
            }
            else
            {
                int stride = width * 4;
                int clipStride = clipWidth * 4;
                Byte[] result = new Byte[checked(clipStride * clipHeight)];

                int clipIndex = 0;
                for (int y = y0; y < (y0 + clipHeight); y++)
                {
                    Array.Copy(
                        pixels, stride * y + x0 * 4,    // source
                        result, clipIndex,              // destination
                        clipWidth * 4
                        );

                    clipIndex += clipWidth * 4;
                }

                return result;
            }
        }

        /// <summary>
        /// Check if an image is of Pbgra format and has pixel with bgr > a.
        /// Such images have to be encoded with pre-multiplied alpha channel to be accurate.
        /// </summary>
        /// <param name="bitmapSource"></param>
        /// <returns></returns>
        internal static bool NeedPremultiplyAlpha(BitmapSource bitmapSource)
        {
            if ((bitmapSource != null) && (bitmapSource.Format ==  PixelFormats.Pbgra32))
            {
                int width  = bitmapSource.PixelWidth;
                int height = bitmapSource.PixelHeight;

                int stride = width * 4;

                Int32Rect rect = new Int32Rect(0, 0, width, 1);
                byte[] pixels = new byte[stride];

                for (int y = 0; y < height; y ++)
                {
                    bitmapSource.CriticalCopyPixels(rect, pixels, stride, 0);

                    int p = 0;

                    for (int x = 0; x < width; x ++)
                    {
                        if ((pixels[p    ] > pixels[p + 3]) ||
                            (pixels[p + 1] > pixels[p + 3]) ||
                            (pixels[p + 2] > pixels[p + 3]))
                        {
                            return true;
                        }

                        p += 4;
                    }

                    rect.Y ++;
                }
            }

            return false;
        }

        /// <summary>
        /// Extract constant alpha out from opacity mask
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="opacity"></param>
        /// <param name="maskBounds"></param>
        /// <returns></returns>
        public static bool ExtractOpacityMaskOpacity(Brush brush, out double opacity, Rect maskBounds)
        {
            ImageBrush ib = brush as ImageBrush;

            opacity = 1;

            if ((ib != null) && (ib.ImageSource != null))
            {
                BitmapSource bs = ib.ImageSource as BitmapSource;

                if ((bs != null) && (ImageProxy.HasAlpha(bs) == 0))
                {
                    BrushProxy bp = BrushProxy.CreateBrush(ib, maskBounds);

                    if (bp != null)
                    {
                        // Bug 1699447: BrushProxy.IsOpaque just checks if viewbox fills up viewport,
                        // Here we need to check if the brush fills the maskBounds. We need to add a
                        // rectangle or region to BrushProxy.IsOpaque later.
                        if (ib.TileMode == TileMode.None)
                        {
                            if (! (bp.Brush as TileBrush).Viewport.Contains(maskBounds))
                            {
                                 return false;
                            }
                        }

                        opacity = bp.Opacity;

                        bp.Opacity = 1;

                        if (bp.IsOpaque())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Brush

        /// <summary>
        /// Create a DrawingBrush which is Opt-out of inheritance
        /// </summary>
        /// <param name="drawing"></param>
        /// <returns></returns>
        public static DrawingBrush CreateNonInheritingDrawingBrush(Drawing drawing)
        {
            DrawingBrush db = new DrawingBrush();

            // Opt-out of inheritance through the new Freezable.
            db.CanBeInheritanceContext = false;
            db.Drawing = drawing;

            return db;
        }


        /// <summary>
        /// Retrieves the bounding box of TileBrush content.
        /// </summary>
        /// <param name="brush">All Avalon brushes accepted, including VisualBrush</param>
        /// <remarks>
        /// DrawingBrush content bounding box may have TopLeft that isn't (0, 0).
        /// </remarks>
        /// <returns>Returns Rect.Empty if no content, or content isn't visible.</returns>
        public static Rect GetTileContentBounds(TileBrush brush)
        {
            Rect bounds = Rect.Empty;

            ImageBrush imageBrush = brush as ImageBrush;

            if (imageBrush != null)
            {
                if (imageBrush.ImageSource != null)
                {
                    bounds = new Rect(0, 0, imageBrush.ImageSource.Width, imageBrush.ImageSource.Height);
                }

                return bounds;
            }

            DrawingBrush drawingBrush = brush as DrawingBrush;

            if (drawingBrush != null)
            {
                if (drawingBrush.Drawing != null)
                {
                    bounds = drawingBrush.Drawing.Bounds;
                }

                return bounds;
            }

            VisualBrush visualBrush = brush as VisualBrush;

            if (visualBrush != null)
            {
                if (visualBrush.Visual != null)
                {
                    UIElement uiElement = visualBrush.Visual as UIElement;
                    if (uiElement != null)
	            {
                        if ((!uiElement.IsArrangeValid || uiElement.NeverMeasured) && visualBrush.AutoLayoutContent)
                        {
                            // VisualBrush.Visual needs to be arranged in order to correctly report it's bounds
                            uiElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            uiElement.Arrange(new Rect(uiElement.DesiredSize));
                        }
		    }
		    
                    bounds = VisualTreeHelper.GetDescendantBounds(visualBrush.Visual);

                    Geometry clip = VisualTreeHelper.GetClip(visualBrush.Visual);

                    if (clip != null)
                    {
                        bounds.Intersect(clip.Bounds);
                    }
                }

                return bounds;
            }

            Debug.Assert(false, "Unhandled TileBrush type");

            return bounds;
        }

        /// <summary>
        /// Gets TileBrush absolute Viewbox.
        /// </summary>
        /// <param name="brush"></param>
        /// <returns>Returns Empty if brush doesn't have absolute viewbox and content is not visible</returns>
        public static Rect GetTileAbsoluteViewbox(TileBrush brush)
        {
            Rect viewbox = brush.Viewbox;

            if (brush.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                Rect content = GetTileContentBounds(brush);

                if (IsRenderVisible(content))
                {
                    viewbox.Scale(content.Width, content.Height);
                    viewbox.Offset(content.Left, content.Top);
                }
                else
                {
                    viewbox = Rect.Empty;
                }
            }

            return viewbox;
        }

        /// <summary>
        /// Gets TileBrush absolute Viewport relative to fill bounds.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds"></param>
        /// <returns>Returns Empty if brush doesn't have absolute viewport and bounds are not visible</returns>
        public static Rect GetTileAbsoluteViewport(TileBrush brush, Rect bounds)
        {
            Rect viewport = brush.Viewport;

            if (brush.ViewportUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                if (IsRenderVisible(bounds))
                {
                    viewport.Scale(bounds.Width, bounds.Height);
                    viewport.Offset(bounds.Left, bounds.Top);
                }
                else
                {
                    viewport = Rect.Empty;
                }
            }

            return viewport;
        }

        /// <summary>
        /// Creates transformation from TileBrush viewbox to viewport, with stretching and alignment
        /// taken into consideration.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="viewbox">Absolute TileBrush viewbox; must be visible</param>
        /// <param name="viewport">Absolute TileBrush viewport; must be visible</param>
        /// <returns></returns>
        /// <remarks>
        /// This can be used to get world-space bounds of TileBrush content.
        /// </remarks>
        public static Matrix CreateViewboxToViewportTransform(TileBrush brush, Rect viewbox, Rect viewport)
        {
            Debug.Assert(IsValidViewbox(viewbox, brush.Stretch != Stretch.None) &&
                         IsRenderVisible(viewport), "Invisible viewbox and/or viewport");

            Matrix transform = Matrix.CreateTranslation(-viewbox.Left, -viewbox.Top);

            // brush stretch
            double scale;
            switch (brush.Stretch)
            {
                case Stretch.Uniform:
                    scale = Math.Min(viewport.Width / viewbox.Width, viewport.Height / viewbox.Height);
                    transform.Scale(scale, scale);
                    break;

                case Stretch.Fill:
                    transform.Scale(viewport.Width / viewbox.Width, viewport.Height / viewbox.Height);
                    break;

                case Stretch.UniformToFill:
                    scale = Math.Max(viewport.Width / viewbox.Width, viewport.Height / viewbox.Height);
                    transform.Scale(scale, scale);
                    break;

                default:
                    // do nothing
                    break;
            }

            // brush alignment
            {
                Rect stretchedViewbox = viewbox;
                stretchedViewbox.Transform(transform);

                double dx = 0;
                double dy = 0;

                switch (brush.AlignmentX)
                {
                    case AlignmentX.Left: dx = viewport.Left - stretchedViewbox.Left; break;
                    case AlignmentX.Center: dx = viewport.Left - stretchedViewbox.Left + (viewport.Width - stretchedViewbox.Width) / 2; break;
                    case AlignmentX.Right: dx = viewport.Right - stretchedViewbox.Right; break;
                }
                switch (brush.AlignmentY)
                {
                    case AlignmentY.Top: dy = viewport.Top - stretchedViewbox.Top; break;
                    case AlignmentY.Center: dy = viewport.Top - stretchedViewbox.Top + (viewport.Height - stretchedViewbox.Height) / 2; break;
                    case AlignmentY.Bottom: dy = viewport.Bottom - stretchedViewbox.Bottom; break;
                }

                transform.Translate(dx, dy);
            }

            return transform;
        }

        /// <summary>
        /// Creates transformation from TileBrush viewbox to viewport, with stretching and alignment
        /// taken into consideration.
        /// </summary>
        /// <param name="brush">Brush with absolute viewbox, viewport.</param>
        /// <returns></returns>
        public static Matrix CreateViewboxToViewportTransform(TileBrush brush)
        {
            Debug.Assert(brush.ViewboxUnits == BrushMappingMode.Absolute);
            Debug.Assert(brush.ViewportUnits == BrushMappingMode.Absolute);

            return CreateViewboxToViewportTransform(
                brush,
                brush.Viewbox,
                brush.Viewport
                );
        }

        /// <summary>
        /// Creates transformation from TileBrush viewbox to viewport, with stretching and alignment
        /// taken into consideration.
        /// </summary>
        /// <param name="brush">Brush with relative or absolute viewbox, viewport.</param>
        /// <param name="bounds">Brush fill bounds used to calculate absolute viewport if needed.</param>
        /// <returns></returns>
        public static Matrix CreateViewboxToViewportTransform(TileBrush brush, Rect bounds)
        {
            return CreateViewboxToViewportTransform(
                brush,
                GetTileAbsoluteViewbox(brush),
                GetTileAbsoluteViewport(brush, bounds)
                );
        }

        #endregion

        #region Font

        [FriendAccessAllowed]
        public static Uri GetFontUri(GlyphTypeface typeface)
        {
            return typeface.FontUri;
        }

        #endregion

        #region Visual

        /// <summary>
        /// Gets Visual transformation with offset included.
        /// </summary>
        /// <param name="visual"></param>
        /// <returns></returns>
        public static Transform GetVisualTransform(Visual visual)
        {
            Transform transform = VisualTreeHelper.GetTransform(visual);
            Vector offset = VisualTreeHelper.GetOffset(visual);

            if (!IsZero(offset.X) || !IsZero(offset.Y))
            {
                if (transform == null)
                {
                    transform = new TranslateTransform(offset.X, offset.Y);
                }
                else
                {
                    Matrix matrix = transform.Value;

                    matrix.Translate(offset.X, offset.Y);

                    transform = new MatrixTransform(matrix);
                }
            }

            return transform;
        }

        #endregion

        #region Visual Rasterization

        #region Private Members

        /// <summary>
        /// Visual rasterization pixel limit.
        /// Rasterization is split into bands if it exceeds this limit.
        /// </summary>
        private const int VisualRasterizeBandPixelLimit = 1600 * 1200;

        /// <summary>
        /// Gets pixel dimensions of a bitmap given device-independent dimensions and DPI.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <param name="pixelWidth"></param>
        /// <param name="pixelHeight"></param>
        private static void GetBitmapPixelDimensions(
            double width,
            double height,
            double dpiX,
            double dpiY,
            out int pixelWidth,
            out int pixelHeight)
        {
            pixelWidth = (int)(width * dpiX / 96.0);
            pixelHeight = (int)(height * dpiY / 96.0);
        }

        /// <summary>
        /// Gets rasterization bitmap information given a Visual's bounds.
        /// </summary>
        /// <param name="visualBounds"></param>
        /// <param name="visualToWorldTransformHint">Used to calculate world-space bounds to determine rasterization size</param>
        /// <param name="bitmapWidth"></param>
        /// <param name="bitmapHeight"></param>
        /// <param name="bitmapDpiX"></param>
        /// <param name="bitmapDpiY"></param>
        /// <returns>Returns false if bounds are invalid or too small for rasterization.</returns>
        private static bool GetVisualRasterizationBitmapInfo(
            Rect visualBounds,
            Matrix visualToWorldTransformHint,
            out double bitmapWidth,
            out double bitmapHeight,
            out double bitmapDpiX,
            out double bitmapDpiY
            )
        {
            bitmapWidth = -1;
            bitmapHeight = -1;
            bitmapDpiX = 0;
            bitmapDpiY = 0;

            if (!Utility.IsRenderVisible(visualBounds))
            {
                // visual bounds invalid
                return false;
            }

            // estimate world bounds of visual to use as bitmap DPI
            Rect approxWorldBounds = visualBounds;
            approxWorldBounds.Transform(visualToWorldTransformHint);

            bitmapDpiX = approxWorldBounds.Width / visualBounds.Width   * Configuration.RasterizationDPI;
            bitmapDpiY = approxWorldBounds.Height / visualBounds.Height * Configuration.RasterizationDPI;

            // render only if pixel dimensions are > 0
            bitmapWidth = visualBounds.Width;
            bitmapHeight = visualBounds.Height;

            int pixelWidth, pixelHeight;
            GetBitmapPixelDimensions(bitmapWidth, bitmapHeight, bitmapDpiX, bitmapDpiY, out pixelWidth, out pixelHeight);

            return (pixelWidth > 0 && pixelHeight > 0);
        }

        /// <summary>
        /// Rasterizes the specified bounds of a Visual to a bitmap.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="visualBounds">Bounds to rectangle to rasterize.</param>
        /// <param name="renderForBitmapEffect"></param>
        /// <param name="bitmapWidth"></param>
        /// <param name="bitmapHeight"></param>
        /// <param name="bitmapDpiX"></param>
        /// <param name="bitmapDpiY"></param>
        /// <param name="bitmapToVisualTransform"></param>
        /// <returns></returns>
        private static BitmapSource RasterizeVisual(
            Visual visual,
            Rect visualBounds,
            double bitmapWidth, double bitmapHeight,
            double bitmapDpiX, double bitmapDpiY,
            out Matrix bitmapToVisualTransform
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterStart);

            // calculate visual-to-bitmap transform
            Matrix visualToBitmapTransform = Matrix.CreateTranslation(-visualBounds.X, -visualBounds.Y);
            visualToBitmapTransform.Scale(bitmapWidth / visualBounds.Width, bitmapHeight / visualBounds.Height);

            // render visual to bitmap
            int pixelWidth, pixelHeight;
            GetBitmapPixelDimensions(bitmapWidth, bitmapHeight, bitmapDpiX, bitmapDpiY, out pixelWidth, out pixelHeight);

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                bitmapDpiX,
                bitmapDpiY,
                PixelFormats.Pbgra32
                );
            BitmapVisualManager visualManager = new BitmapVisualManager(bitmap);

            visualManager.Render(visual, visualToBitmapTransform, Rect.Empty);

            // calculate bitmap-to-visual transform
            bitmapToVisualTransform = visualToBitmapTransform;
            bitmapToVisualTransform.Invert();

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterEnd);

            return bitmap;
        }

        private static Visual CreateVisualFromDrawing(Drawing drawing)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawDrawing(drawing);
            }

            return visual;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Information on rendering a band of a Visual.
        /// </summary>
        public struct RenderedVisualBand
        {
            public RenderedVisualBand(BitmapSource bitmap, Matrix bitmapToVisualTransform)
            {
                Bitmap = bitmap;
                BitmapToVisualTransform = bitmapToVisualTransform;
            }

            /// <summary>
            /// Rasterized visual.
            /// </summary>
            public BitmapSource Bitmap;

            /// <summary>
            /// Transformation from bitmap to Visual/Drawing space.
            /// </summary>
            public Matrix BitmapToVisualTransform;
        }

        /// <summary>
        /// Rasterizes a Visual in its entirety to a bitmap.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="visualBounds"></param>
        /// <param name="visualToWorldTransformHint">Used to calculate world-space bounds to determine rasterization size</param>
        /// <param name="bitmapToVisualTransform"></param>
        /// <returns></returns>
        public static BitmapSource RasterizeVisual(
            Visual visual,
            Rect visualBounds,
            Matrix visualToWorldTransformHint,
            out Matrix bitmapToVisualTransform
            )
        {
            double bitmapWidth, bitmapHeight;
            double bitmapDpiX, bitmapDpiY;

            bitmapToVisualTransform = Matrix.Identity;

            if (!GetVisualRasterizationBitmapInfo(
                visualBounds,
                visualToWorldTransformHint,
                out bitmapWidth,
                out bitmapHeight,
                out bitmapDpiX,
                out bitmapDpiY))
            {
                // bounds invalid or too small
                return null;
            }

            return RasterizeVisual(
                visual,
                visualBounds,
                bitmapWidth, bitmapHeight,
                bitmapDpiX, bitmapDpiY,
                out bitmapToVisualTransform
                );
        }

        /// <summary>
        /// Rasterizes a Drawing in its entirety to a bitmap.
        /// </summary>
        /// <param name="drawing"></param>
        /// <param name="drawingBounds"></param>
        /// <param name="drawingToWorldTransformHint">Used to calculate world-space bounds to determine rasterization size</param>
        /// <param name="bitmapToDrawingTransform"></param>
        /// <returns></returns>
        public static BitmapSource RasterizeDrawing(
            Drawing drawing,
            Rect drawingBounds,
            Matrix drawingToWorldTransformHint,
            out Matrix bitmapToDrawingTransform
            )
        {
            double bitmapWidth, bitmapHeight;
            double bitmapDpiX, bitmapDpiY;

            bitmapToDrawingTransform = Matrix.Identity;

            if (!GetVisualRasterizationBitmapInfo(
                drawingBounds,
                drawingToWorldTransformHint,
                out bitmapWidth,
                out bitmapHeight,
                out bitmapDpiX,
                out bitmapDpiY))
            {
                // bounds invalid or too small
                return null;
            }

            return RasterizeVisual(
                CreateVisualFromDrawing(drawing),
                drawingBounds,
                bitmapWidth, bitmapHeight,
                bitmapDpiX, bitmapDpiY,
                out bitmapToDrawingTransform
                );
        }

        #endregion

        #endregion

        #region Input Checking

        /// <summary>
        /// Returns true if Rect when rendered would be visible.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        /// <remarks>
        /// A rectangle is visible if it does not contain NaN, is finite, and has positive area.
        ///
        /// Warning: Be careful when using to cull geometry. Ensure that the bounds you
        /// are testing include the stroke, as it is possible to have visible output with
        /// zero-area geometry and a pen.
        /// </remarks>
        public static bool IsRenderVisible(Rect rect)
        {
            bool result = false;

            if (!rect.IsEmpty)
            {
                result = IsValid(rect) && IsFinite(rect) && rect.Width > 0 && rect.Height > 0;
            }

            return result;
        }

        /// <summary>
        /// Returns true if Rect is valid Viewbox
        /// Viewbox is used for setup a transformation by design. When Stretch == None, it's not used for
        /// source clipping, so viewbox can have zero height/width.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="hasStretch"></param>
        /// <returns></returns>
        /// <remarks>
        /// A rectangle is valid viewbox if it does not contain NaN, is finite, and has non negative width/height.
        /// </remarks>
        public static bool IsValidViewbox(Rect rect, bool hasStretch)
        {
            if (hasStretch)
            {
                return IsRenderVisible(rect);
            }
            else
            {
                return IsValid(rect) && IsFinite(rect) && rect.Width >= 0 && rect.Height >= 0;
            }
        }

        public static bool IsRenderVisible(Point point)
        {
            return IsValid(point) && IsFinite(point);
        }

        public static bool IsRenderVisible(Size size)
        {
            return IsValid(size) && IsFinite(size) && size.Width > 0 && size.Height > 0;
        }

        public static bool IsRenderVisible(double value)
        {
            return IsValid(value) && IsFinite(value);
        }

        public static bool IsRenderVisible(DrawingGroup drawing)
        {
            if (Utility.IsTransparent(Utility.NormalizeOpacity(drawing.Opacity)))
                return false;

            if (drawing.Children == null || drawing.Children.Count == 0)
                return false;

            if (drawing.ClipGeometry != null && !Utility.IsRenderVisible(drawing.ClipGeometry.Bounds))
                return false;

            if (drawing.Transform != null && !Utility.IsValid(drawing.Transform.Value))
                return false;

            if (BrushProxy.IsEmpty(drawing.OpacityMask))
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the value is valid input.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Currently validity means the value is not NaN.
        /// </remarks>
        public static bool IsValid(double value)
        {
            return !double.IsNaN(value);
        }

        public static bool IsValid(Point point)
        {
            return !double.IsNaN(point.X) && !double.IsNaN(point.Y);
        }

        /// <summary>
        /// Returns true if Size is valid.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        /// <remarks>Empty size is treated as valid.</remarks>
        public static bool IsValid(Size size)
        {
            return !double.IsNaN(size.Width) && !double.IsNaN(size.Height);
        }

        /// <summary>
        /// Returns true if Rect is valid.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        /// <remarks>Empty rect is treated as valid.</remarks>
        public static bool IsValid(Rect rect)
        {
            return !double.IsNaN(rect.X) &&
                !double.IsNaN(rect.Y) &&
                !double.IsNaN(rect.Width) &&
                !double.IsNaN(rect.Height);
        }

        /// <summary>
        /// Returns true if Matrix is valid.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static bool IsValid(Matrix matrix)
        {
            return matrix.HasInverse &&
                !double.IsNaN(matrix.M11) &&
                !double.IsNaN(matrix.M12) &&
                !double.IsNaN(matrix.M21) &&
                !double.IsNaN(matrix.M22) &&
                !double.IsNaN(matrix.OffsetX) &&
                !double.IsNaN(matrix.OffsetY);
        }

        public static bool IsFinite(double value)
        {
            return !double.IsInfinity(value);
        }

        public static bool IsFinite(Point point)
        {
            return !double.IsInfinity(point.X) && !double.IsInfinity(point.Y);
        }

        public static bool IsFinite(Size size)
        {
            return !double.IsInfinity(size.Width) && !double.IsInfinity(size.Height);
        }

        public static bool IsFinite(Rect rect)
        {
            return !double.IsInfinity(rect.X) &&
                !double.IsInfinity(rect.Y) &&
                !double.IsInfinity(rect.Width) &&
                !double.IsInfinity(rect.Height);
        }

        /// <summary>
        /// Normalizes opacity/alpha value to [0, 1].
        /// </summary>
        /// <param name="value"></param>
        /// <param name="goodValue"></param>
        /// <returns></returns>
        public static double NormalizeOpacity(double value, double goodValue)
        {
            if (double.IsNaN(value) || value <= 0)
            {
                return 0;       // NaN is treated as 0
            }
            else if (value >= 1)
            {
                return 1;
            }
            else
            {
                return goodValue;
            }
        }

        /// <summary>
        /// Normalizes color channels to [0, 1] if NaN or they exceed limits.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="goodValue"></param>
        /// <returns></returns>
        private static float NormalizeColorChannel(float value, float goodValue)
        {
            if (float.IsNaN(value) || value < (float)XpsMinDouble)
            {
                return 0;
            }
            else if (value > (float)XpsMaxDouble)
            {
                return 1;
            }
            else
            {
                return goodValue;
            }
        }

        public static double NormalizeOpacity(double value)
        {
            return NormalizeOpacity(value, value);
        }

        private static float NormalizeColorChannel(float value)
        {
            return NormalizeColorChannel(value, value);
        }

        /// <summary>
        /// Normalizes if outside range, otherwise returns NaN if no normalization needed.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float NormalizeOpacityIfChanged(float value)
        {
            return (float)NormalizeOpacity(value, double.NaN);
        }

        private static float NormalizeColorChannelIfChanged(float value)
        {
            return NormalizeColorChannel(value, float.NaN);
        }

        /// <summary>
        /// Normalizes Color so its components are in the range [0, 1].
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color NormalizeColor(Color color)
        {
            float a = NormalizeOpacityIfChanged(color.ScA);
            float r = NormalizeColorChannelIfChanged(color.ScR);
            float g = NormalizeColorChannelIfChanged(color.ScG);
            float b = NormalizeColorChannelIfChanged(color.ScB);

            if (!float.IsNaN(a))
            {
                color.ScA = a;
            }

            if (!float.IsNaN(r))
            {
                color.ScR = r;
            }

            if (!float.IsNaN(g))
            {
                color.ScG = g;
            }

            if (!float.IsNaN(b))
            {
                color.ScB = b;
            }

            return color;
        }

        #endregion

        #region Debugging

        /// <summary>
        /// Indicates if a debug header should be printed at the top of GDI output.
        /// </summary>
        /// <remarks>
        /// Accessed by GDIExporter!CGDIDevice.HrEndPage.
        /// </remarks>
        [FriendAccessAllowed]
        public static bool DisplayPageDebugHeader
        {
            get
            {
                return Configuration.DisplayPageDebugHeader;
            }
        }

        #endregion
    }

    #region GeometryAnalyzer

    /// <summary>
    /// Analyzes Geometry through StreamGeometryContext implementation.
    /// </summary>
    internal class GeometryAnalyzer : CapacityStreamGeometryContext
    {
        #region Constructors

        /// <summary>
        /// Constructs analyzer.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="checkRectangular">Non-null to test for rectangular geometry</param>
        private GeometryAnalyzer(Matrix transform, Rect? checkRectangular)
        {
            _transform = transform;

            if (checkRectangular.HasValue)
            {
                Rect rect = checkRectangular.Value;

                Debug.Assert(!rect.IsEmpty);

                _rectDirection = TraversalDirection.None;
                _rect = rect;
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Analyzes geometry for point count.
        /// </summary>
        /// <param name="geometryData"></param>
        /// <param name="estimatedPointCount"></param>
        public static void Analyze(Geometry.PathGeometryData geometryData, out int estimatedPointCount)
        {
            GeometryAnalyzer analyzer = new GeometryAnalyzer(Matrix.Identity, null);

            PathGeometry.ParsePathGeometryData(geometryData, analyzer);

            analyzer.FinishAnalysis();

            estimatedPointCount = analyzer.EstimatedPointCount;
        }

        /// <summary>
        /// Performs more complex analysis of geometry.
        /// </summary>
        /// <param name="geometryData"></param>
        /// <param name="checkRectangular">If not null, checks if geometry is the provided rectangle</param>
        /// <param name="estimatedPointCount">Estimated number of points in geometry</param>
        /// <param name="isRectangle">True to indicate that geometry is guaranteed to be a rectangle</param>
        /// <param name="isLineSegment">True to indicate that geometry is one line</param>
        /// <returns></returns>
        /// <remarks>
        /// A geometry with one closed figure containing one segment is considered to
        /// have two line segments.
        /// </remarks>
        public static void Analyze(
            Geometry.PathGeometryData geometryData,
            Rect? checkRectangular,
            out int estimatedPointCount,
            out bool isRectangle,
            out bool isLineSegment
            )
        {
            Matrix transform = Matrix.Identity;

            if (checkRectangular.HasValue)
            {
                // transform geometry to coordinate space of checkRectangular to allow for comparison
                transform = System.Windows.Media.Composition.CompositionResourceManager.MilMatrix3x2DToMatrix(
                    ref geometryData.Matrix
                    );
            }

            GeometryAnalyzer analyzer = new GeometryAnalyzer(transform, checkRectangular);

            PathGeometry.ParsePathGeometryData(geometryData, analyzer);

            analyzer.FinishAnalysis();

            estimatedPointCount = analyzer.EstimatedPointCount;
            isRectangle = analyzer.IsRectangle;
            isLineSegment = analyzer.IsLineSegment;
        }

        #endregion

        #region Private Fields

        private int _count;

        // transformation to apply to incoming geometry data; currently only used when checking rectangular
        private Matrix _transform;

        // Check if geometry is a rectangle when != Invalid.
        // Rectangle checking can handle CW/CCW rectangles and intermediate points not at corners.
        private TraversalDirection _rectDirection = TraversalDirection.Invalid;
        private Rect _rect;

        private Point _rectFirstPointUntransformed;  // first point traversed, not transformed by _transform
        private Point _rectLastPoint;   // last point traversed, transformed
        private int _rectCornerCount;   // number of rectangle corners traversed

        // Check if geometry is exactly one line segment.
        private int _lineSegmentCount;

        // Upper bound to GDI point count.
        private int EstimatedPointCount
        {
            get
            {
                return _count;
            }
        }

        // If true, geometry is guaranteed to be a rectangle.
        private bool IsRectangle
        {
            get
            {
                return _rectDirection != TraversalDirection.Invalid;
            }
        }

        /// <summary>
        /// If true, geometry is guaranteed to be a single line segment.
        /// </summary>
        private bool IsLineSegment
        {
            get
            {
                return _lineSegmentCount == 1;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finishes geometry analysis. Call after PathGeometry.ParsePathGeometryData returns.
        /// </summary>
        private void FinishAnalysis()
        {
            if (IsRectangle)
            {
                // traverse back to start point to finish rectangle check
                CheckRectanglePoint(_rectFirstPointUntransformed);

                if (_rectCornerCount != 4)
                {
                    SetNotRectangle();
                }
            }
        }

        #endregion

        #region Rectangle Checking Methods

        /// <summary>
        /// Returns true if specified point is on a rectangle corner.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool IsRectangleCorner(Point point)
        {
            return
                (Utility.AreClose(point.X, _rect.Left) || Utility.AreClose(point.X, _rect.Right)) &&
                (Utility.AreClose(point.Y, _rect.Top) || Utility.AreClose(point.Y, _rect.Bottom));
        }

        /// <summary>
        /// Starts rectangle checking with first point.
        /// </summary>
        /// <param name="point"></param>
        private void CheckRectangleStart(Point point)
        {
            _rectFirstPointUntransformed = point;
            _rectLastPoint = _transform.Transform(point);

            if (IsRectangleCorner(_rectLastPoint))
            {
                _rectCornerCount++;
            }
        }

        /// <summary>
        /// Checks if geometry point is part of rectangle.
        /// </summary>
        /// <param name="point"></param>
        private void CheckRectanglePoint(Point point)
        {
            //
            // The idea is to get direction we're walking the rectangle compared to last point.
            // Direction changes imply we're at a corner, and a rectangle has 4 corners.
            //
            point = _transform.Transform(point);

            TraversalDirection dir = GetDirection(point - _rectLastPoint);

            if (dir != TraversalDirection.None &&
                _rectDirection != TraversalDirection.None &&
                dir != _rectDirection)
            {
                // direction change must occur at corner, and new direction must be perpendicular
                // to old direction
                if (IsRectangleCorner(point) && ArePerpendicularDirections(dir, _rectDirection))
                {
                    _rectCornerCount++;
                }
                else
                {
                    SetNotRectangle();
                }
            }

            // Avoid overwriting _rectDirection if SetNotRectangle() is called in above
            if (IsRectangle)
            {
                _rectDirection = dir;
            }
            _rectLastPoint = point;
        }

        /// <summary>
        /// Indicates geometry is not a rectangle.
        /// </summary>
        private void SetNotRectangle()
        {
            _rectDirection = TraversalDirection.Invalid;
        }

        #endregion

        #region Line Checking Methods

        /// <summary>
        /// Adds another line segment count.
        /// </summary>
        private void AddLineSegments(int count)
        {
            _lineSegmentCount += count;
        }

        /// <summary>
        /// Indicates geometry is not a single line segment.
        /// </summary>
        private void SetNotLineSegment()
        {
            _lineSegmentCount = 2;  // too many segments to be a single line segment
        }

        #endregion

        #region Traversal Direction Members

        private enum TraversalDirection
        {
            Right = 0, Down = 1, Left = 2, Up = 3,

            Invalid,            // geometry is not a rectangle
            None,               // no direction, point duplicates previous point or haven't reached 2nd point yet

            FirstValid = Right,
            LastValid = Up,
        }

        /// <summary>
        /// Given a vector, gets direction in which we're traversing the rectangle.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private static TraversalDirection GetDirection(Vector vector)
        {
            bool xzero = Utility.IsZero(vector.X);
            bool yzero = Utility.IsZero(vector.Y);

            if (xzero && yzero)
            {
                // no change in direction
                return TraversalDirection.None;
            }
            else if (xzero)
            {
                // traversing vertically
                return (vector.Y < 0) ? TraversalDirection.Up : TraversalDirection.Down;
            }
            else if (yzero)
            {
                // traversing horizontally
                return (vector.X < 0) ? TraversalDirection.Left : TraversalDirection.Right;
            }
            else
            {
                // traversing diagonally, can't be a rectangle
                return TraversalDirection.Invalid;
            }
        }

        /// <summary>
        /// Returns true if travel directions are perpendicular.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        private static bool ArePerpendicularDirections(TraversalDirection d1, TraversalDirection d2)
        {
            if (d1 == TraversalDirection.Invalid || d2 == TraversalDirection.Invalid)
            {
                // invalid directions are never perpendicular
                return false;
            }

            int v = (int)d1;
            int prev = (d2 == TraversalDirection.FirstValid) ? (int)TraversalDirection.LastValid : ((int)d2 - 1);
            int next = (d2 == TraversalDirection.LastValid) ? (int)TraversalDirection.FirstValid : ((int)d2 + 1);

            return (v == prev) || (v == next);
        }

        #endregion

        #region CapacityStreamGeometryContext Members

        // CapacityStreamGeometryContext Members
        public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
        {
            // start point, and possible manual close via PT_LINETO
            _count += 2;

            if (IsRectangle)
            {
                CheckRectangleStart(startPoint);
            }

            if (isClosed)
            {
                // a closed figure either generates zero or two segments (first segment + closing segment)
                SetNotLineSegment();
            }
        }

        public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
        {
            _count++;

            if (IsRectangle)
            {
                CheckRectanglePoint(point);
            }

            AddLineSegments(1);
        }

        public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
        {
            SetNotRectangle();
            SetNotLineSegment();

            // converted to 3 bezier points
            _count += 3;
        }

        public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
        {
            SetNotRectangle();
            SetNotLineSegment();

            _count += 3;
        }

        public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            _count += points.Count;

            if (IsRectangle)
            {
                for (int index = 0; index < points.Count && IsRectangle; index++)
                {
                    CheckRectanglePoint(points[index]);
                }
            }

            AddLineSegments(points.Count);
        }

        public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            SetNotRectangle();
            SetNotLineSegment();

            _count += (points.Count + 1) / 2 * 3;
        }

        public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            SetNotRectangle();
            SetNotLineSegment();

            _count += (points.Count + 2) / 3 * 3;
        }

        public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
        {
            SetNotRectangle();
            SetNotLineSegment();

            // An arc can be converted to a maxiumum of 4 Bezier segments, Check ArcToBezier in DrawingContextFlattener.cs
            _count += 4 * 3;
        }

        internal override void SetClosedState(bool closed)
        {
        }

        internal override void SetFigureCount(int figureCount)
        {
            if (figureCount != 1)
            {
                // assume more than one figure is not rectangle, not single line
                SetNotRectangle();
                SetNotLineSegment();
            }
        }

        internal override void SetSegmentCount(int segmentCount)
        {
            if (segmentCount != 1)
            {
                SetNotLineSegment();
            }
        }

        #endregion
    }

    #endregion
}
