// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using MS.Internal;
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
    /// This is the Geometry class for Rectangles and RoundedRectangles. 
    /// </summary>   
    public sealed partial class RectangleGeometry : Geometry
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RectangleGeometry()
        {
        }

        /// <summary>
        /// Constructor - sets the rounded rectangle to equal the passed in parameters
        /// </summary>
        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        /// <summary>
        /// Constructor - sets the rounded rectangle to equal the passed in parameters
        /// </summary>
        public RectangleGeometry(Rect rect,
            double radiusX,
            double radiusY) : this(rect)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="transform"></param>
        public RectangleGeometry(
            Rect rect,
            double radiusX,
            double radiusY,
            Transform transform) : this(rect, radiusX, radiusY)
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

                Rect currentRect = Rect;
                Transform transform = Transform;

                if (currentRect.IsEmpty)
                {
                    boundsRect = Rect.Empty;
                }
                else if (transform == null || transform.IsIdentity)
                {
                    boundsRect = currentRect;
                }
                else 
                {
                    double radiusX = RadiusX;
                    double radiusY = RadiusY;

                    if (radiusX == 0 && radiusY == 0)
                    {
                        boundsRect = currentRect;
                        transform.TransformRect(ref boundsRect);
                    }
                    else
                    {
                        //
                        // Transformed rounded rectangles are more tricky. 
                        //
                        // If at sometime in the
                        // future this code gets excerised enough, we can
                        // handle the general case in managed code. Until then,
                        // it's easier to let unmanaged code do the work for us.
                        //

                        Matrix geometryMatrix;

                        Transform.GetTransformValue(transform, out geometryMatrix);

                        boundsRect = RectangleGeometry.GetBoundsHelper(
                            null /* no pen */,
                            Matrix.Identity,
                            currentRect,
                            radiusX,
                            radiusY,
                            geometryMatrix,
                            StandardFlatteningTolerance, 
                            ToleranceType.Absolute);
                    }
                }

                return boundsRect;
            }
        }

        internal override bool AreClose(Geometry geometry)
        {
            RectangleGeometry rectGeometry2 = geometry as RectangleGeometry;

            if (rectGeometry2 != null)
            {
                RectangleGeometry rectGeometry1 = this;
                Rect rect1 = rectGeometry1.Rect;
                Rect rect2 = rectGeometry2.Rect;
                
                return (
                    DoubleUtil.AreClose(rect1.X, rect2.X) &&
                    DoubleUtil.AreClose(rect1.Y, rect2.Y) &&
                    DoubleUtil.AreClose(rect1.Width, rect2.Width) &&
                    DoubleUtil.AreClose(rect1.Height, rect2.Height) &&
                    DoubleUtil.AreClose(rectGeometry1.RadiusX, rectGeometry2.RadiusX) &&
                    DoubleUtil.AreClose(rectGeometry1.RadiusY, rectGeometry2.RadiusY) &&
                    (rectGeometry1.Transform == rectGeometry2.Transform) &&
                    (rectGeometry1.IsFrozen == rectGeometry2.IsFrozen)
                    );
            }

            return base.AreClose(geometry);
        }

        /// <summary>
        /// Returns the axis-aligned bounding rectangle when stroked with a pen, after applying
        /// the supplied transform (if non-null).
        /// </summary>
        internal override Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            Matrix geometryMatrix;
            
            Transform.GetTransformValue(Transform, out geometryMatrix);

            return RectangleGeometry.GetBoundsHelper(
                pen,
                worldMatrix,
                Rect,
                RadiusX,
                RadiusY,
                geometryMatrix,
                tolerance,
                type);
        }
        
        internal static Rect GetBoundsHelper(Pen pen, Matrix worldMatrix, Rect rect, double radiusX, double radiusY,
                                             Matrix geometryMatrix, double tolerance, ToleranceType type)
        {
            Rect boundingRect;

            Debug.Assert(worldMatrix != null);
            Debug.Assert(geometryMatrix != null);

            if (rect.IsEmpty)
            {
                boundingRect = Rect.Empty;
            }
            else if ( (pen == null || pen.DoesNotContainGaps) &&
                geometryMatrix.IsIdentity && worldMatrix.IsIdentity)
            {
                double strokeThickness = 0.0;

                boundingRect = rect;

                if (Pen.ContributesToBounds(pen))
                {
                    strokeThickness = Math.Abs(pen.Thickness);

                    boundingRect.X -= 0.5*strokeThickness;
                    boundingRect.Y -= 0.5*strokeThickness;
                    boundingRect.Width += strokeThickness;
                    boundingRect.Height += strokeThickness;
                }
            }
            else
            {
                unsafe
                {
                    uint pointCount, segmentCount;
                    GetCounts(rect, radiusX, radiusY, out pointCount, out segmentCount);

                    // We've checked that rect isn't empty above
                    Invariant.Assert(pointCount != 0);

                    Point * pPoints = stackalloc Point[(int)pointCount];
                    RectangleGeometry.GetPointList(pPoints, pointCount, rect, radiusX, radiusY);

                    fixed (byte *pTypes = RectangleGeometry.GetTypeList(rect, radiusX, radiusY))
                    {
                        boundingRect = Geometry.GetBoundsHelper(
                            pen,
                            &worldMatrix,
                            pPoints,
                            pTypes,
                            pointCount,
                            segmentCount,
                            &geometryMatrix,
                            tolerance,
                            type,
                            false);  // skip hollows - meaningless here, this is never a hollow
                    }
                }
            }

            return boundingRect;
        }

        internal override bool ContainsInternal(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            if (IsEmpty())
            {
                return false;
            }

            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            uint pointCount = GetPointCount(rect, radiusX, radiusY);
            uint segmentCount = GetSegmentCount(rect, radiusX, radiusY);
            
            unsafe
            {
                Point *pPoints = stackalloc Point[(int)pointCount];
                RectangleGeometry.GetPointList(pPoints, pointCount, rect, radiusX, radiusY);

                fixed (byte* pTypes = GetTypeList(rect, radiusX, radiusY))
                {
                    return ContainsInternal(
                        pen,
                        hitPoint,
                        tolerance, 
                        type,
                        pPoints,
                        pointCount,
                        pTypes,
                        segmentCount);
                }
            }
        }

        /// <summary>
        /// Gets the area of this geometry
        /// </summary>
        /// <param name="tolerance">The computational error tolerance</param>
        /// <param name="type">The way the error tolerance will be interpreted - relative or absolute</param>
        public override double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();
                
            if (IsEmpty())
            {
                return 0.0;
            }

            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            // Get the area of the bounding rectangle
            double area = Math.Abs(rect.Width * rect.Height);

            // correct it for the rounded corners
            area -= Math.Abs(radiusX * radiusY) * (4.0 - Math.PI);                 
            
            // Adjust to internal transformation
            Transform transform = Transform;
            if (!transform.IsIdentity)
            {
                area *= Math.Abs(transform.Value.Determinant);
            }

            return area;
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            if (IsEmpty())
            {
                return null;
            }

            // Combine the transform argument with the internal transform
            Matrix matrix = GetCombinedMatrix(transform);

            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            if (IsRounded(radiusX, radiusY))
            {
                Point[] points = GetPointList(rect, radiusX, radiusY);

                // Transform if applicable.
                if (!matrix.IsIdentity)
                {
                    for (int i=0; i<points.Length; i++)
                    {
                        points[i] *= matrix;
                    }
                }

                PathFigureCollection collection = new PathFigureCollection();
                collection.Add(
                    new PathFigure(
                    points[0],
                    new PathSegment[]{
                        new BezierSegment(points[1], points[2], points[3], true, true),
                        new LineSegment(points[4], true, true),
                        new BezierSegment(points[5], points[6], points[7], true, true),
                        new LineSegment(points[8], true, true),
                        new BezierSegment(points[9], points[10], points[11], true, true),
                        new LineSegment(points[12], true, true),
                        new BezierSegment(points[13], points[14], points[15], true, true)},
                        true    // closed
                    )
                );

                return collection;
            }
            else
            {                
                PathFigureCollection collection = new PathFigureCollection();
                collection.Add(
                    new PathFigure(
                    rect.TopLeft * matrix,
                    new PathSegment[]{
                        new PolyLineSegment(
                        new Point[]
                        {
                            rect.TopRight * matrix,
                            rect.BottomRight * matrix,
                            rect.BottomLeft * matrix
                        },
                        true)},
                        true    // closed
                    )
                );

                return collection;
            }            
        }
        
        internal static bool IsRounded(double radiusX, double radiusY)
        {
            return (radiusX != 0.0) && (radiusY != 0.0);
        }

        internal bool IsRounded()
        {
            return RadiusX != 0.0
                && RadiusY != 0.0;
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

            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            ByteStreamGeometryContext ctx = new ByteStreamGeometryContext();

            if (IsRounded(radiusX, radiusY))
            {
                Point[] points = GetPointList(rect, radiusX, radiusY);

                ctx.BeginFigure(points[0], true /* is filled */, true /* is closed */);
                ctx.BezierTo(points[1], points[2], points[3], true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(points[4], true /* is stroked */, false /* is smooth join */);
                ctx.BezierTo(points[5], points[6], points[7], true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(points[8], true /* is stroked */, false /* is smooth join */);
                ctx.BezierTo(points[9], points[10], points[11], true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(points[12], true /* is stroked */, false /* is smooth join */);
                ctx.BezierTo(points[13], points[14], points[15], true /* is stroked */, false /* is smooth join */);
            }
            else
            {   
                ctx.BeginFigure(rect.TopLeft, true /* is filled */, true /* is closed */);
                ctx.LineTo(Rect.TopRight, true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(Rect.BottomRight, true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(Rect.BottomLeft, true /* is stroked */, false /* is smooth join */);
            }

            ctx.Close();
            data.SerializedData = ctx.GetData();

            return data;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Point[] GetPointList(Rect rect, double radiusX, double radiusY)
        {
            uint pointCount = GetPointCount(rect, radiusX, radiusY);
            Point[] points = new Point[pointCount];

            unsafe
            {
                fixed(Point *pPoints = points)
                {
                    RectangleGeometry.GetPointList(pPoints, pointCount, rect, radiusX, radiusY);
                }
            }

            return points;
        }

        private unsafe static void GetPointList(Point * points, uint pointsCount, Rect rect, double radiusX, double radiusY)
        {
            if (IsRounded(radiusX, radiusY))
            {
                // It is a rounded rectangle
                Invariant.Assert(pointsCount >= c_roundedPointCount);

                radiusX = Math.Min(rect.Width * (1.0 / 2.0), Math.Abs(radiusX));
                radiusY = Math.Min(rect.Height * (1.0 / 2.0), Math.Abs(radiusY));

                double bezierX = ((1.0 - EllipseGeometry.c_arcAsBezier) * radiusX);
                double bezierY = ((1.0 - EllipseGeometry.c_arcAsBezier) * radiusY);

                points[1].X = points[0].X = points[15].X = points[14].X = rect.X;
                points[2].X = points[13].X = rect.X + bezierX;
                points[3].X = points[12].X = rect.X + radiusX;
                points[4].X = points[11].X = rect.Right - radiusX;
                points[5].X = points[10].X = rect.Right - bezierX;
                points[6].X = points[7].X = points[8].X = points[9].X = rect.Right;

                points[2].Y = points[3].Y = points[4].Y = points[5].Y = rect.Y;
                points[1].Y = points[6].Y = rect.Y + bezierY;
                points[0].Y = points[7].Y = rect.Y + radiusY;
                points[15].Y = points[8].Y = rect.Bottom - radiusY;
                points[14].Y = points[9].Y = rect.Bottom - bezierY;
                points[13].Y = points[12].Y = points[11].Y = points[10].Y = rect.Bottom;

                points[16] = points[0];
            }
            else
            {
                // The rectangle is not rounded
                Invariant.Assert(pointsCount >= c_squaredPointCount);

                points[0].X = points[3].X = points[4].X = rect.X;
                points[1].X = points[2].X = rect.Right;

                points[0].Y = points[1].Y = points[4].Y = rect.Y;
                points[2].Y = points[3].Y = rect.Bottom;
            }
        }

        private static byte[] GetTypeList(Rect rect, double radiusX, double radiusY)
        {
            if (rect.IsEmpty)
            {
                return null;
            }
            else if (IsRounded(radiusX, radiusY))
            {
                return s_roundedPathTypes;
            }
            else
            {
                return s_squaredPathTypes;
            }
        }

        private uint GetPointCount(Rect rect, double radiusX, double radiusY)
        {
            if (rect.IsEmpty)
            {
                return 0;
            }
            else if (IsRounded(radiusX, radiusY))
            {
                return c_roundedPointCount;
            }
            else
            {
                return c_squaredPointCount;
            }
        }

        private uint GetSegmentCount(Rect rect, double radiusX, double radiusY)
        {
            if (rect.IsEmpty)
            {
                return 0;
            }
            else if (IsRounded(radiusX, radiusY))
            {
                return c_roundedSegmentCount;
            }
            else
            {
                return c_squaredSegmentCount;
            }
        }

        private static void GetCounts(Rect rect, double radiusX, double radiusY, out uint pointCount, out uint segmentCount)
        {
            if (rect.IsEmpty)
            {
                pointCount = 0;
                segmentCount = 0;
            }
            else if (IsRounded(radiusX, radiusY))
            {
                // The rectangle is rounded
                pointCount = c_roundedPointCount;
                segmentCount = c_roundedSegmentCount;
            }
            else
            {
                pointCount = c_squaredPointCount;
                segmentCount = c_squaredSegmentCount;
            }
        }

        #region Public Methods

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            return Rect.IsEmpty;
        }

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            return IsRounded();
        }

        #endregion Public Methods

        #region InstanceData

        // Rouneded
        static private UInt32 c_roundedSegmentCount = 8;
        static private UInt32 c_roundedPointCount = 17;

        static private byte smoothBezier = (byte)MILCoreSegFlags.SegTypeBezier |
                                            (byte)MILCoreSegFlags.SegIsCurved   |
                                            (byte)MILCoreSegFlags.SegSmoothJoin;

        static private byte smoothLine = (byte)MILCoreSegFlags.SegTypeLine | (byte)MILCoreSegFlags.SegSmoothJoin;

        static private byte[] s_roundedPathTypes = {
            (byte)MILCoreSegFlags.SegTypeBezier | 
            (byte)MILCoreSegFlags.SegIsCurved   |
            (byte)MILCoreSegFlags.SegSmoothJoin | 
            (byte)MILCoreSegFlags.SegClosed,
            smoothLine, 
            smoothBezier,
            smoothLine, 
            smoothBezier,
            smoothLine, 
            smoothBezier,
            smoothLine 
        };

        // Squared
        private const UInt32 c_squaredSegmentCount = 4;
        private const UInt32 c_squaredPointCount = 5;

        private static readonly byte[] s_squaredPathTypes = {
            (byte)MILCoreSegFlags.SegTypeLine | (byte)MILCoreSegFlags.SegClosed,
            (byte)MILCoreSegFlags.SegTypeLine,
            (byte)MILCoreSegFlags.SegTypeLine,
            (byte)MILCoreSegFlags.SegTypeLine
        };

        #endregion
    }
}

