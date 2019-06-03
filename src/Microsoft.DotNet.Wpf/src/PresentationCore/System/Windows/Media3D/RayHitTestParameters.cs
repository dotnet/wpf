// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using MS.Internal.Media3D;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Encapsulates a set parameters for performing a 3D hit test agaist
    ///     a ray.
    /// </summary>
    public sealed class RayHitTestParameters : HitTestParameters3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a RayHitTestParameters where the ray is described
        ///     by an origin and a direction.
        /// </summary>
        public RayHitTestParameters(Point3D origin, Vector3D direction)
        {
            _origin = origin;
            _direction = direction;
            _isRay = true;
        }

        #endregion Constructors
    
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The origin of the ray to be used for hit testing.
        /// </summary>
        public Point3D Origin
        {
            get { return _origin; }
        }

        /// <summary>
        ///     The direction of the ray to be used for hit testing.
        /// </summary>
        public Vector3D Direction
        {
            get { return _direction; }
        }

        #endregion Public Properties

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

        #region Internal Methods

        internal void ReportResult(
            MeshGeometry3D meshHit,
            Point3D pointHit,
            double distanceToRayOrigin, 
            int vertexIndex1,
            int vertexIndex2,
            int vertexIndex3,
            Point barycentric)
        {
            results.Add(new RayMeshGeometry3DHitTestResult(
                CurrentVisual,
                CurrentModel,
                meshHit,
                pointHit,
                distanceToRayOrigin,
                vertexIndex1,
                vertexIndex2,
                vertexIndex3,
                barycentric));
        }
        
        internal HitTestResultBehavior RaiseCallback(HitTestResultCallback resultCallback, 
                                                     HitTestFilterCallback filterCallback,
                                                     HitTestResultBehavior lastResult)
        {
            return RaiseCallback(resultCallback, filterCallback, lastResult, 0.0 /* distance adjustment */);
        }

        internal HitTestResultBehavior RaiseCallback(HitTestResultCallback resultCallback, 
                                                     HitTestFilterCallback filterCallback, 
                                                     HitTestResultBehavior lastResult, 
                                                     double distanceAdjustment)
        {
            results.Sort(RayHitTestResult.CompareByDistanceToRayOrigin);

            for(int i = 0, count = results.Count; i < count; i++)
            {
                RayHitTestResult result = results[i];

                result.SetDistanceToRayOrigin(result.DistanceToRayOrigin + distanceAdjustment);
                
                Viewport2DVisual3D viewport2DVisual3D = result.VisualHit as Viewport2DVisual3D;
                if (viewport2DVisual3D != null)
                {
                    Point intersectionPoint;
                    Visual viewport2DVisual3DChild = viewport2DVisual3D.Visual;

                    if (viewport2DVisual3DChild != null)
                    {
                        if (Viewport2DVisual3D.GetIntersectionInfo(result, out intersectionPoint))
                        {
                            // convert the resulting point to visual coordinates
                            Point visualPoint = Viewport2DVisual3D.TextureCoordsToVisualCoords(intersectionPoint, viewport2DVisual3DChild);
                            GeneralTransform gt = viewport2DVisual3DChild.TransformToOuterSpace().Inverse;

                            Point pointOnChild;
                            if (gt != null && gt.TryTransform(visualPoint, out pointOnChild))
                            {
                                HitTestResultBehavior behavior2D = viewport2DVisual3DChild.HitTestPoint(filterCallback, 
                                                                                                        resultCallback, 
                                                                                                        new PointHitTestParameters(pointOnChild));

                                if (behavior2D == HitTestResultBehavior.Stop)
                                {
                                    return HitTestResultBehavior.Stop;
                                }                            
                            }
                        }
                    }
                }
                
                HitTestResultBehavior behavior = resultCallback(results[i]);

                if (behavior == HitTestResultBehavior.Stop)
                {
                    return HitTestResultBehavior.Stop;
                }
            }

            return lastResult;
        }
                
        // Gets the hit testing line/ray specified as an origin and direction in
        // the current local space.
        internal void GetLocalLine(out Point3D origin, out Vector3D direction)
        {
            origin = _origin;
            direction = _direction;

            bool isRay = true;
            
            if (HasWorldTransformMatrix)
            {   
                LineUtil.Transform(WorldTransformMatrix, ref origin, ref direction, out isRay);
            }

            // At any point along the tree walk we may encounter a transform that turns the ray into
            // a line and if so we must stay a line
            _isRay &= isRay;
        }

        internal void ClearResults()
        {
            if (results != null)
            {
                results.Clear();
            }
        }

        #endregion Internal Methods

        internal bool IsRay
        {
            get
            {
                return _isRay;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly Point3D _origin;
        private readonly Vector3D _direction;
        private readonly List<RayHitTestResult> results = new List<RayHitTestResult>();
        // 'true' if this is a ray hit test, 'false' if the ray has become a line
        private bool _isRay;

        #endregion Private Fields
    }
}

