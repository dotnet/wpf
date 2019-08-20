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
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices; 
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media 
{
    /// <summary>
    /// This is the Geometry class for Lines. 
    /// </summary>
    public sealed partial class LineGeometry : Geometry 
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public LineGeometry()
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        public LineGeometry(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public LineGeometry(
            Point startPoint,
            Point endPoint,
            Transform transform) : this(startPoint, endPoint)
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

                Rect rect = new Rect(StartPoint, EndPoint);

                Transform transform = Transform;
                
                if (transform != null && !transform.IsIdentity) 
                {
                    transform.TransformRect(ref rect);
                }

                return rect;
            }
        }

        /// <summary>
        /// Returns the axis-aligned bounding rectangle when stroked with a pen, after applying
        /// the supplied transform (if non-null).
        /// </summary>
        internal override Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            Matrix geometryMatrix;
            
            Transform.GetTransformValue(Transform, out geometryMatrix);

            return LineGeometry.GetBoundsHelper(
                   pen,
                   worldMatrix,
                   StartPoint,
                   EndPoint,
                   geometryMatrix,
                   tolerance,
                   type);
        }

        internal static Rect GetBoundsHelper(Pen pen, Matrix worldMatrix, Point pt1, Point pt2,
                                             Matrix geometryMatrix, double tolerance, ToleranceType type)
        {
            Debug.Assert(worldMatrix != null);
            Debug.Assert(geometryMatrix != null);

            if (pen == null  &&  worldMatrix.IsIdentity && geometryMatrix.IsIdentity)
            {
                return new Rect(pt1, pt2);
            }
            else
            {
                unsafe
                {
                    Point* pPoints = stackalloc Point[2];
                    pPoints[0] = pt1;
                    pPoints[1] = pt2;

                    fixed (byte *pTypes = LineGeometry.s_lineTypes)
                    {
                        return Geometry.GetBoundsHelper(
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
        }

        internal override bool ContainsInternal(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            unsafe
            {
                Point *pPoints = stackalloc Point[2];
                pPoints[0] = StartPoint;
                pPoints[1] = EndPoint;
                
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
            return false;
        }

        /// <summary>
        /// Gets the area of this geometry
        /// </summary>
        /// <param name="tolerance">The computational error tolerance</param>
        /// <param name="type">The way the error tolerance will be interpreted - relative or absolute</param>
        public override double GetArea(double tolerance, ToleranceType type)
        {
            return 0.0;
        }

        private byte[] GetTypeList() { return s_lineTypes; }

        private static byte[] s_lineTypes = new byte[] { (byte)MILCoreSegFlags.SegTypeLine };

        private uint GetPointCount() { return c_pointCount; }

        private uint GetSegmentCount() { return c_segmentCount; }

        /// <summary>
        /// GetAsPathGeometry - return a PathGeometry version of this Geometry
        /// </summary>
        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule.EvenOdd, Transform);
            PathGeometry.ParsePathGeometryData(GetPathGeometryData(), ctx);

            return ctx.GetPathGeometry();
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            // This is lossy for consistency with other GetPathFigureCollection() implementations
            // however this limitation doesn't otherwise need to exist for LineGeometry.

            Point startPoint = StartPoint;
            Point endPoint = EndPoint;

            // Apply internal transform
            Transform internalTransform = Transform;

            if (internalTransform != null && !internalTransform.IsIdentity)
            {
                Matrix matrix = internalTransform.Value;

                startPoint *= matrix;
                endPoint *= matrix;
            }

            // Apply external transform
            if (transform != null && !transform.IsIdentity)
            {
                Matrix matrix = transform.Value;

                startPoint *= matrix;
                endPoint *= matrix;
            }

            PathFigureCollection collection = new PathFigureCollection();
            collection.Add(
                new PathFigure(
                startPoint,
                new PathSegment[]{new LineSegment(endPoint, true)},
                false // ==> not closed
                )
            );

            return collection;
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

            ByteStreamGeometryContext ctx = new ByteStreamGeometryContext();

            ctx.BeginFigure(StartPoint, true /* is filled */, false /* is closed */);
            ctx.LineTo(EndPoint, true /* is stroked */, false /* is smooth join */);
            
            ctx.Close();
            data.SerializedData = ctx.GetData();

            return data;
        }

        #region Static Data
        
        private const UInt32 c_segmentCount = 1;
        private const UInt32 c_pointCount = 2;

        #endregion
    }
}

