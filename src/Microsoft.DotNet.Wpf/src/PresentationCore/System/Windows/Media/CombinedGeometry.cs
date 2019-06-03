// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of CombinedGeometry
//
//      2004/11/11-Michka
//          Created it
//

using System;
using MS.Internal;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// CombinedGeometry
    /// </summary>
    public sealed partial class CombinedGeometry : Geometry
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public CombinedGeometry()
        {
        }

        /// <summary>
        /// Constructor from 2 operands
        /// </summary>
        /// <param name="geometry1"> 
        /// First geometry to combine
        /// </param>
        /// <param name="geometry2"> 
        /// Second geometry to combine
        /// </param>
        public CombinedGeometry(
            Geometry geometry1,
            Geometry geometry2
        )
        {
            Geometry1 = geometry1;
            Geometry2 = geometry2;
        }

        /// <summary>
        /// Constructor from combine mode and 2 operands
        /// </summary>
        /// <param name="geometryCombineMode"> 
        /// Combine mode - Union, Intersect, Exclude or Xor
        /// </param>
        /// <param name="geometry1"> 
        /// First geometry to combine
        /// </param>
        /// <param name="geometry2"> 
        /// Second geometry to combine
        /// </param>
        public CombinedGeometry(
            GeometryCombineMode geometryCombineMode,
            Geometry geometry1,
            Geometry geometry2
        )
        {
            GeometryCombineMode = geometryCombineMode;
            Geometry1 = geometry1;
            Geometry2 = geometry2;
        }

        /// <summary>
        /// Constructor from combine mode, 2 operands and a transformation
        /// </summary>
        /// <param name="geometryCombineMode"> 
        /// Combine mode - Union, Intersect, Exclude or Xor
        /// </param>
        /// <param name="geometry1"> 
        /// First geometry to combine
        /// </param>
        /// <param name="geometry2"> 
        /// Second geometry to combine
        /// </param>
        /// <param name="transform"> 
        /// Transformation to apply to the result
        /// </param>
        public CombinedGeometry(
            GeometryCombineMode geometryCombineMode,
            Geometry geometry1,
            Geometry geometry2,
            Transform transform)
        {
            GeometryCombineMode = geometryCombineMode;
            Geometry1 = geometry1;
            Geometry2 = geometry2;
            Transform = transform;
        }

        #endregion

        #region Bounds
        /// <summary>
        /// Gets the bounds of this Geometry as an axis-aligned bounding box
        /// </summary>
        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                // GetAsPathGeometry() checks if the geometry is valid
                return GetAsPathGeometry().Bounds;
            }
        }
        #endregion

        #region GetBoundsInternal
        /// <summary>
        /// Gets the bounds of this Geometry as an axis-aligned bounding box given a Pen and/or Transform
        /// </summary>
        internal override Rect GetBoundsInternal(Pen pen, Matrix matrix, double tolerance, ToleranceType type)
        { 
            if (IsObviouslyEmpty()) 
            {
                return Rect.Empty;
            }

            return GetAsPathGeometry().GetBoundsInternal(pen, matrix, tolerance, type);
        }
        #endregion

        #region Hit Testing
        /// <summary>
        /// Returns if point is inside the filled geometry.
        /// </summary>
        internal override bool ContainsInternal(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            if (pen == null)
            {
                ReadPreamble();

                // Hit the two operands
                bool hit1 = false;
                bool hit2 = false;

                Transform transform = Transform;
                if (transform != null && !transform.IsIdentity)
                {
                    // Inverse-transform the hit point
                    Matrix matrix = transform.Value;
                    if (matrix.HasInverse)
                    {
                        matrix.Invert();
                        hitPoint *= matrix;
                    }
                    else
                    {
                        // The matrix will collapse the geometry to nothing, containing nothing 
                        return false;
                    }
                }

                Geometry geometry1 = Geometry1;
                Geometry geometry2 = Geometry2;
                if (geometry1 != null)
                {
                    hit1 = geometry1.ContainsInternal(pen, hitPoint, tolerance, type);
                }
                if (geometry2 != null)
                {
                    hit2 = geometry2.ContainsInternal(pen, hitPoint, tolerance, type);
                }

                // Determine containment according to the theoretical definition
                switch (GeometryCombineMode)
                {
                    case GeometryCombineMode.Union:
                        return hit1 || hit2;

                    case GeometryCombineMode.Intersect:
                        return hit1 && hit2;

                    case GeometryCombineMode.Exclude:
                        return hit1 && !hit2;

                    case GeometryCombineMode.Xor:
                        return hit1 != hit2;
                }

                // We should have returned from one of the cases
                Debug.Assert(false);
                return false;
            }
            else
            {
                // pen != null
                return base.ContainsInternal(pen, hitPoint, tolerance, type);
            }
        }

        #endregion

        /// <summary>
        /// Gets the area of this geometry
        /// </summary>
        /// <param name="tolerance">The computational error tolerance</param>
        /// <param name="type">The way the error tolerance will be interpreted - realtive or absolute</param>
        public override double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();

            // Potential speedup, to be done if proved important:  As the result of a Combine
            // operation, the result of GetAsPathGeometry() is guaranteed to be organized into
            // flattened well oriented figures.  Its area can therefore be computed much faster
            // without the heavy machinary of CArea.  This will require writing an internal 
            // CShapeBase::GetRawArea method, and a utility to invoke it.  For now:
            return GetAsPathGeometry().GetArea(tolerance, type);
        }

        #region Internal

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            return GetAsPathGeometry().GetTransformedFigureCollection(transform);
        }

        /// <summary>
        /// GetPathGeometryData - returns a struct which contains this Geometry represented
        /// as a path geometry's serialized format.
        /// </summary>
        internal override PathGeometryData GetPathGeometryData()
        {
            if (IsObviouslyEmpty())
            {
                return Geometry.GetEmptyPathGeometryData();
            }

            PathGeometry pathGeometry = GetAsPathGeometry();

            return pathGeometry.GetPathGeometryData();
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            // Get the operands, interpreting null as empty PathGeometry
            Geometry g1 = Geometry1;
            Geometry g2 = Geometry2;
            PathGeometry geometry1 = (g1 == null) ?
                new PathGeometry() :
                g1.GetAsPathGeometry();

            Geometry geometry2 = (g2 == null) ?
                new PathGeometry() :
                g2.GetAsPathGeometry();

            // Combine them and return the result
            return Combine(geometry1, geometry2, GeometryCombineMode, Transform);
        }

        #endregion

        #region IsEmpty

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            return GetAsPathGeometry().IsEmpty();
        }
        
        internal override bool IsObviouslyEmpty()
        {
            // See which operand is obviously empty
            Geometry geometry1 = Geometry1;
            Geometry geometry2 = Geometry2;
            bool empty1 = geometry1 == null || geometry1.IsObviouslyEmpty();
            bool empty2 = geometry2 == null || geometry2.IsObviouslyEmpty();
            
            // Depending on the operation -- 
            if (GeometryCombineMode == GeometryCombineMode.Intersect)
            {
                return empty1 || empty2;
            }
            else if (GeometryCombineMode == GeometryCombineMode.Exclude)
            {
                return empty1;
            }
            else
            {
                // Union or Xor
                return empty1 && empty2;
            }
        }


        #endregion IsEmpty

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            Geometry geometry1 = Geometry1;
            Geometry geometry2 = Geometry2;
            return ((geometry1 != null) && geometry1.MayHaveCurves())
                ||
                   ((geometry2 != null) && geometry2.MayHaveCurves());
        }
    }
}
