// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     The HitTestResult of a Visual3D.HitTest(...) where the parameter
    ///     was a RayHitTestParameter and the ray intersected a MeshGeometry3D.
    /// 
    ///     NOTE:  This might have originated as a PointHitTest on a 2D Visual
    ///            which was extended into 3D.
    /// </summary>
    public sealed class RayMeshGeometry3DHitTestResult : RayHitTestResult
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal RayMeshGeometry3DHitTestResult(
            Visual3D visualHit,
            Model3D modelHit,
            MeshGeometry3D meshHit,
            Point3D pointHit,
            double distanceToRayOrigin, 
            int vertexIndex1,
            int vertexIndex2,
            int vertexIndex3,
            Point barycentricCoordinate) : base (visualHit, modelHit)
            {
                _meshHit = meshHit;
                _pointHit = pointHit;
                _distanceToRayOrigin = distanceToRayOrigin;
                _vertexIndex1 = vertexIndex1;
                _vertexIndex2 = vertexIndex2;
                _vertexIndex3 = vertexIndex3;
                _barycentricCoordinate = barycentricCoordinate;
            }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     This is a point in 3-space at which the ray intersected
        ///     the geometry of the hit Model3D.  This point is in the
        ///     coordinate system of the Visual3D.
        /// </summary>
        public override Point3D PointHit
        {
            get
            {
                return _pointHit;
            }
        }

        /// <summary>
        ///     This is the distance between the ray's origin and the
        ///     point the PointHit.
        /// </summary>
        public override double DistanceToRayOrigin
        {
            get { return _distanceToRayOrigin; }
        }

        /// <Summary>
        ///     Index of the 1st vertex of the triangle which was intersected.
        ///     Use this to retrieve the position, texturecoordinate, etc. from
        ///     the MeshHit.
        /// </Summary>
        public int VertexIndex1
        {
            get { return _vertexIndex1; }
        }

        /// <Summary>
        ///     Index of the 2nd vertex of the triangle which was intersected.
        ///     Use this to retrieve the position, texturecoordinate, etc. from
        ///     the MeshHit.
        /// </Summary>
        public int VertexIndex2
        {
            get { return _vertexIndex2; }
        }

        /// <Summary>
        ///     Index of the 3rd vertex of the triangle which was intersected.
        ///     Use this to retrieve the position, texturecoordinate, etc. from
        ///     the MeshHit.
        /// </Summary>
        public int VertexIndex3
        {
            get { return _vertexIndex3; }
        }

        /// <Summary />
        public double VertexWeight1
        {
            get { return 1 - VertexWeight2 - VertexWeight3; }
        }

        /// <Summary />
        public double VertexWeight2
        {
            get { return _barycentricCoordinate.X; }
        }

        /// <Summary />
        public double VertexWeight3
        {
            get { return _barycentricCoordinate.Y; }
        }

        /// <Summary>
        ///     The MeshGeometry3D which was intersected by the ray.
        /// </Summary>
        public MeshGeometry3D MeshHit
        {
            get { return _meshHit; }
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal override void SetDistanceToRayOrigin(double distance)
        {
            _distanceToRayOrigin = distance;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private double _distanceToRayOrigin; // Not readonly because it can be adjusted after construction.
        private readonly int _vertexIndex1;
        private readonly int _vertexIndex2;
        private readonly int _vertexIndex3;
        private readonly Point _barycentricCoordinate;
        private readonly MeshGeometry3D _meshHit;
        private readonly Point3D _pointHit;

        #endregion Private Fields
    }
}
