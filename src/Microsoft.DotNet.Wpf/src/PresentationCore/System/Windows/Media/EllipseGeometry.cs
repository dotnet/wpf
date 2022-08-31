// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using MS.Internal;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media 
{
    /// <summary>
    /// This is the Geometry class for Circles and Ellipses 
    /// </summary>
    public sealed partial class EllipseGeometry : Geometry 
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public EllipseGeometry()
        {
        }

        /// <summary>
        /// Constructor - sets the ellipse to the paramters with the given transformation
        /// </summary>
        public EllipseGeometry(Rect rect)
        {
            if (rect.IsEmpty) 
            {
                throw new System.ArgumentException(SR.Get(SRID.Rect_Empty, "rect"));
            }

            RadiusX = (rect.Right - rect.X) * (1.0 / 2.0);
            RadiusY = (rect.Bottom - rect.Y) * (1.0 / 2.0);
            Center = new Point(rect.X + RadiusX, rect.Y + RadiusY);
        }
        
        /// <summary>
        /// Constructor - sets the ellipse to the parameters
        /// </summary>
        public EllipseGeometry(
            Point center, 
            double radiusX, 
            double radiusY)
        {
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }
        
        /// <summary>
        /// Constructor - sets the ellipse to the parameters
        /// </summary>
        public EllipseGeometry(
            Point center,
            double radiusX,
            double radiusY,
            Transform transform) : this(center, radiusX, radiusY)
        {
            Transform = transform;
        }

        #endregion

        /// <summary>
        /// Gets the bounds of this Geometry as an axis-aligned bounding box
        /// </summary>
        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                Rect boundsRect;

                Transform transform = Transform;

                if (transform == null || transform.IsIdentity) 
                {
                    Point currentCenter = Center;
                    Double currentRadiusX = RadiusX;
                    Double currentRadiusY = RadiusY;

                    boundsRect = new Rect(
                        currentCenter.X - Math.Abs(currentRadiusX),
                        currentCenter.Y - Math.Abs(currentRadiusY),
                        2.0 * Math.Abs(currentRadiusX),
                        2.0 * Math.Abs(currentRadiusY));
                }
                else
                {
                    //
                    // If at sometime in the
                    // future this code gets exercised enough, we can
                    // handle the general case in managed code. Until then,
                    // it's easier to let unmanaged code do the work for us.
                    //

                    Matrix geometryMatrix;

                    Transform.GetTransformValue(transform, out geometryMatrix);

                    boundsRect = EllipseGeometry.GetBoundsHelper(
                        null /* no pen */,
                        Matrix.Identity,
                        Center,
                        RadiusX,
                        RadiusY,
                        geometryMatrix,
                        StandardFlatteningTolerance, 
                        ToleranceType.Absolute);
                }

                return boundsRect;
            }
}

        /// <summary>
        /// Returns the axis-aligned bounding rectangle when stroked with a pen, after applying
        /// the supplied transform (if non-null).
        /// </summary>
        internal override Rect GetBoundsInternal(Pen pen, Matrix matrix, double tolerance, ToleranceType type)
        {
            Matrix geometryMatrix;
            
            Transform.GetTransformValue(Transform, out geometryMatrix);

            return EllipseGeometry.GetBoundsHelper(
                pen,
                matrix,
                Center,
                RadiusX,
                RadiusY,
                geometryMatrix,
                tolerance,
                type);
        }
        
        internal static Rect GetBoundsHelper(Pen pen, Matrix worldMatrix, Point center, double radiusX, double radiusY,
                                             Matrix geometryMatrix, double tolerance, ToleranceType type)
        {
            Rect rect;

            if ( (pen == null || pen.DoesNotContainGaps) &&
                worldMatrix.IsIdentity && geometryMatrix.IsIdentity)
            {
                double strokeThickness = 0.0;

                if (Pen.ContributesToBounds(pen))
                {
                    strokeThickness = Math.Abs(pen.Thickness);
                }

                rect = new Rect(
                    center.X - Math.Abs(radiusX)-0.5*strokeThickness,
                    center.Y - Math.Abs(radiusY)-0.5*strokeThickness,
                    2.0 * Math.Abs(radiusX)+strokeThickness,
                    2.0 * Math.Abs(radiusY)+strokeThickness);
            }
            else
            {
                unsafe
                {
                    Point * pPoints = stackalloc Point[(int)c_pointCount];
                    EllipseGeometry.GetPointList(pPoints, c_pointCount, center, radiusX, radiusY);

                    fixed (byte *pTypes = EllipseGeometry.s_roundedPathTypes)
                    {
                        rect = Geometry.GetBoundsHelper(
                            pen, 
                            &worldMatrix, 
                            pPoints, 
                            pTypes, 
                            c_pointCount, 
                            c_segmentCount,
                            &geometryMatrix,
                            tolerance,
                            type,
                            false); // skip hollows - meaningless here, this is never a hollow
                    }
                }
            }

            return rect;
        }

        internal override bool ContainsInternal(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            unsafe
            {
                Point *pPoints = stackalloc Point[(int)GetPointCount()];
                EllipseGeometry.GetPointList(pPoints, GetPointCount(), Center, RadiusX, RadiusY);

                fixed (byte* pTypes = GetTypeList())
                {
                    return ContainsInternal(
                        pen,
                        hitPoint,
                        tolerance, 
                        type,
                        pPoints,
                        GetPointCount(),
                        pTypes,
                        GetSegmentCount());
                }
            }
        }

        #region Public Methods

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            return false;
        }

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            return true;
        }

        /// <summary>
        /// Gets the area of this geometry
        /// </summary>
        /// <param name="tolerance">The computational error tolerance</param>
        /// <param name="type">The way the error tolerance will be interpreted - realtive or absolute</param>
        public override double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();

            double area = Math.Abs(RadiusX * RadiusY) * Math.PI;

            // Adjust to internal transformation
            Transform transform = Transform;
            if (transform != null && !transform.IsIdentity)
            {
                area *= Math.Abs(transform.Value.Determinant);
            }

            return area;
        }

        #endregion Public Methods

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            Point [] points = GetPointList();

            // Get the combined transform argument with the internal transform
            Matrix matrix = GetCombinedMatrix(transform);
            if (!matrix.IsIdentity)
            {
                for (int i=0; i<points.Length; i++)
                {
                    points[i] *= matrix;
                }
            }

            PathFigureCollection figureCollection = new PathFigureCollection();
            figureCollection.Add(
                new PathFigure(
                    points[0],
                    new PathSegment[]{
                    new BezierSegment(points[1], points[2], points[3], true, true),
                    new BezierSegment(points[4], points[5], points[6], true, true),
                    new BezierSegment(points[7], points[8], points[9], true, true),
                    new BezierSegment(points[10], points[11], points[12], true, true)},
                    true
                    )
                );

            return figureCollection;
        }

        /// <summary>
        /// GetAsPathGeometry - return a PathGeometry version of this Geometry
        /// </summary>
        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule.EvenOdd, Transform);
            PathGeometry.ParsePathGeometryData(GetPathGeometryData(), ctx);

            return ctx.GetPathGeometry();
        }

        /// <summary>
        /// GetPathGeometryData - returns a byte[] which contains this Geometry represented
        /// as a path geometry's serialized format.
        /// </summary>
        internal override PathGeometryData GetPathGeometryData()
        {
            if (IsObviouslyEmpty())
            {
                return Geometry.GetEmptyPathGeometryData();
            }

            PathGeometryData data = new PathGeometryData();
            data.FillRule = FillRule.EvenOdd;
            data.Matrix = CompositionResourceManager.TransformToMilMatrix3x2D(Transform);

            Point[] points = GetPointList();

            ByteStreamGeometryContext ctx = new ByteStreamGeometryContext();

            ctx.BeginFigure(points[0], true /* is filled */, true /* is closed */);

            // i == 0, 3, 6, 9
            for (int i = 0; i < 12; i += 3)
            {
                ctx.BezierTo(points[i + 1], points[i + 2], points[i + 3], true /* is stroked */, true /* is smooth join */);
            }

            ctx.Close();
            data.SerializedData = ctx.GetData();

            return data;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Point[] GetPointList()
        {
            Point[] points = new Point[GetPointCount()];

            unsafe
            {
                fixed(Point *pPoints = points)
                {
                    EllipseGeometry.GetPointList(pPoints, GetPointCount(), Center, RadiusX, RadiusY);
                }
            }

            return points;
        }

        private unsafe static void GetPointList(Point * points, uint pointsCount, Point center, double radiusX, double radiusY)
        {
            Invariant.Assert(pointsCount >= c_pointCount);

            radiusX = Math.Abs(radiusX);
            radiusY = Math.Abs(radiusY);
              
            // Set the X coordinates
            double mid = radiusX * c_arcAsBezier;

            points[0].X = points[1].X = points[11].X = points[12].X = center.X + radiusX;
            points[2].X = points[10].X = center.X + mid;
            points[3].X = points[9].X = center.X;
            points[4].X = points[8].X = center.X - mid;
            points[5].X = points[6].X = points[7].X = center.X - radiusX;

            // Set the Y coordinates
            mid = radiusY * c_arcAsBezier;

            points[2].Y = points[3].Y = points[4].Y = center.Y + radiusY;
            points[1].Y = points[5].Y = center.Y + mid;
            points[0].Y = points[6].Y = points[12].Y = center.Y;
            points[7].Y = points[11].Y = center.Y - mid;
            points[8].Y = points[9].Y = points[10].Y = center.Y - radiusY;
        }

        private byte[] GetTypeList() { return s_roundedPathTypes; }
        private uint GetPointCount() { return c_pointCount; }
        private uint GetSegmentCount() { return c_segmentCount; }
        
        #region Static Data
        
        // Approximating a 1/4 circle with a Bezier curve                _
        internal const double c_arcAsBezier = 0.5522847498307933984; // =( \/2 - 1)*4/3

        private const UInt32 c_segmentCount = 4;
        private const UInt32 c_pointCount = 13;

        private const byte c_smoothBezier = (byte)MILCoreSegFlags.SegTypeBezier  |
                                              (byte)MILCoreSegFlags.SegIsCurved    |
                                              (byte)MILCoreSegFlags.SegSmoothJoin;

        private static readonly byte[] s_roundedPathTypes = {
            (byte)MILCoreSegFlags.SegTypeBezier | 
            (byte)MILCoreSegFlags.SegIsCurved   |
            (byte)MILCoreSegFlags.SegSmoothJoin | 
            (byte)MILCoreSegFlags.SegClosed,
            c_smoothBezier, 
            c_smoothBezier, 
            c_smoothBezier
        };

        #endregion
    }
}

