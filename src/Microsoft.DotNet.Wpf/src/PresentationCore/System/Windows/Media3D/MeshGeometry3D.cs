// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D mesh implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using MS.Internal;
using MS.Internal.Media3D;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Diagnostics;
using System.Windows.Markup;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     MeshGeometry3D a straightforward triangle primitive.
    /// </summary>
    public sealed partial class MeshGeometry3D : Geometry3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        ///     Default Constructor.
        /// </summary>
        public MeshGeometry3D() {}

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Get bounds for this MeshGeometry3D.
        /// </summary>
        public override Rect3D Bounds
        {
            get
            {
                ReadPreamble();

                if (_cachedBounds.IsEmpty)
                {
                    UpdateCachedBounds();
                }

                Debug_VerifyCachedBounds();

                return _cachedBounds;
            }
        }
        
        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        
        #region Protected Methods
        
        /// <summary>
        ///     Overriden to clear our bounds cache.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                DependencyProperty dp = e.Property;
                // We invalidate the cache here rather than in the InvalidateResourcePositions method
                // because the later is not invoked in the event that the Point3DCollection is swapped
                // out from underneath us.  (In that case, the resource invalidation takes a different
                // code path.)
            
                if (dp == MeshGeometry3D.PositionsProperty)
                {
                    SetCachedBoundsDirty();
                }
            }
            
            base.OnPropertyChanged(e);
        }
        
        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
      
        internal Rect GetTextureCoordinateBounds()
        {
            PointCollection tx = TextureCoordinates;
 
            int count = (tx == null) ? 0 : tx.Count;

            if (count > 0)
            {
                Point ptMin = tx[0];
                Point ptMax = tx[0];
    
                for (int i = 1; i < count; i++)
                {
                    Point txPt = tx.Internal_GetItem(i);
                    double txx = txPt.X;
                    
                    if (ptMin.X > txx)
                    {
                        ptMin.X = txx;
                    }
                    else if (ptMax.X < txx)
                    {
                        ptMax.X = txx;
                    }

                    double txy = txPt.Y;

                    if (ptMin.Y > txy)
                    {
                        ptMin.Y = txy;
                    }
                    else if (ptMax.Y < txy)
                    {
                        ptMax.Y = txy;
                    }
                }
    
                return new Rect(ptMin, ptMax);
            }
            else
            {
                return Rect.Empty;
            }
        }

        //
        // Hits the ray against the mesh
        //
        internal override void RayHitTestCore(
            RayHitTestParameters rayParams,
            FaceType hitTestableFaces)
        {
            Debug.Assert(hitTestableFaces != FaceType.None, 
                "Caller should make sure we're trying to hit something");

            Point3DCollection positions = Positions;
            if (positions == null)
            {
                return;
            }

            Point3D origin;
            Vector3D direction;

            rayParams.GetLocalLine(out origin, out direction);

            Int32Collection indices = TriangleIndices;

            // In the line case, we want to hit test all faces because we don't
            // have a direction. This may differ from what faces we want to
            // accept.
            FaceType facesToHit;
            if (rayParams.IsRay)
            {
                facesToHit = hitTestableFaces;
            }
            else
            {
                facesToHit = FaceType.Front | FaceType.Back;
            }

            
            //
            // This code duplication is unfortunate but necessary. Breaking it down into methods 
            // further significantly impacts performance. About 5% improvement could be made
            // by unrolling this code below even more.
            //
            // If futher perf investigation is done with this code, be sure to test NGEN assemblies only
            // as JIT produces different, faster code than NGEN.
            //
            
            if (indices == null || indices.Count == 0)
            {
                FrugalStructList<Point3D> ps = positions._collection; 
                int count = ps.Count - (ps.Count % 3);
                
                for (int i = count - 1; i >= 2; i -= 3)
                {
                    int i0 = i - 2;
                    int i1 = i - 1;
                    int i2 = i;
                    
                    Point3D v0 = ps[i0];
                    Point3D v1 = ps[i1];
                    Point3D v2 = ps[i2];
                
                    double hitTime;
                    Point barycentric;

                    // The line hit test is equivalent to a double sided
                    // triangle hit because it doesn't cull triangles based
                    // on winding
                    if (LineUtil.ComputeLineTriangleIntersection(
                            facesToHit,
                            ref origin,
                            ref direction,
                            ref v0,
                            ref v1,
                            ref v2,
                            out barycentric,
                            out hitTime
                            )
                        )
                    {        
                        if (rayParams.IsRay)
                        {                          
                            ValidateRayHit(
                                rayParams, 
                                ref origin, 
                                ref direction, 
                                hitTime,
                                i0,
                                i1,
                                i2,
                                ref barycentric
                                );
                        }
                        else
                        {
                            ValidateLineHit(
                                rayParams, 
                                hitTestableFaces, 
                                i0,
                                i1,
                                i2,
                                ref v0,
                                ref v1,
                                ref v2,
                                ref barycentric
                                );
                        }
                    }
                }
}
            else // indexed mesh
            {
                FrugalStructList<Point3D> ps = positions._collection;
                FrugalStructList<int> idcs = indices._collection;
                
                int count = idcs.Count;
                int limit = ps.Count;
                                          
                for (int i = 2; i < count; i += 3)
                {
                    int i0 = idcs[i - 2];
                    int i1 = idcs[i - 1];
                    int i2 = idcs[i];
                
                    // Quit if we encounter an index out of range.
                    // This is okay because the triangles we ignore are not rendered.
                    //  (see: CMilMeshGeometry3DDuce::Realize)
                    if ((0 > i0 || i0 >= limit) ||
                        (0 > i1 || i1 >= limit) ||
                        (0 > i2 || i2 >= limit))
                    {
                        break;
                    }
                
                    Point3D v0 = ps[i0];
                    Point3D v1 = ps[i1];
                    Point3D v2 = ps[i2];
                
                    double hitTime;
                    Point barycentric;
                    
                    if (LineUtil.ComputeLineTriangleIntersection(
                            facesToHit,
                            ref origin,
                            ref direction,
                            ref v0,
                            ref v1,
                            ref v2,
                            out barycentric,
                            out hitTime
                            )
                        )
                    {        
                        if (rayParams.IsRay)
                        {   
                            ValidateRayHit(
                                rayParams, 
                                ref origin, 
                                ref direction, 
                                hitTime,
                                i0,
                                i1,
                                i2,
                                ref barycentric
                                );
                        }
                        else
                        {
                            ValidateLineHit(
                                rayParams, 
                                hitTestableFaces, 
                                i0,
                                i1,
                                i2,
                                ref v0,
                                ref v1,
                                ref v2,
                                ref barycentric
                                );
                        }
                    }
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        #region Private Methods

        //
        // Processes a ray-triangle intersection to see if it's a valid hit. Unnecessary faces
        // have already been culled by the ray-triange intersection routines.
        // 
        // Shares some code with ValidateLineHit
        //
        private void ValidateRayHit(
            RayHitTestParameters rayParams, 
            ref Point3D origin, 
            ref Vector3D direction, 
            double hitTime,
            int i0,
            int i1,
            int i2,
            ref Point barycentric
            )
        {
            if (hitTime > 0)
            {
                Matrix3D worldTransformMatrix = rayParams.HasWorldTransformMatrix ? rayParams.WorldTransformMatrix : Matrix3D.Identity;

                Point3D pointHit = origin + hitTime * direction;
                
                Point3D worldPointHit = pointHit;
                worldTransformMatrix.MultiplyPoint(ref worldPointHit);
                
                // If we have a HitTestProjectionMatrix than this hit test originated
                // at a Viewport3DVisual.
                if (rayParams.HasHitTestProjectionMatrix)
                {
                    // To test if we are in front of the far clipping plane what we
                    // do conceptually is project our hit point in world space into
                    // homogenous space and verify that it is on the correct side of
                    // the Z=1 plane.
                    //
                    // To save some cycles we only bother computing Z and W of the
                    // projected point and use a simple Z/W > 1 test to see if we
                    // are past the far plane.
                    //
                    // NOTE: HitTestProjectionMatrix is not just the camera matrices.
                    //       It has an additional translation to move the ray to the
                    //       origin.  This extra translation does not effect this test.
                    
                    Matrix3D m = rayParams.HitTestProjectionMatrix;

                    // We directly substitute 1 for p.W below:
                    double pz = worldPointHit.X * m.M13 + worldPointHit.Y * m.M23 + worldPointHit.Z * m.M33 + m.OffsetZ;
                    double pw = worldPointHit.X * m.M14 + worldPointHit.Y * m.M24 + worldPointHit.Z * m.M34 + m.M44;

                    // Early exit if pz/pw > 1.  The negated logic is to reject NaNs.
                    if (!(pz / pw <= 1))
                    {
                        return;
                    }

                    Debug.Assert(!double.IsInfinity(pz / pw) && !double.IsNaN(pz / pw),
                        "Expected near/far tests to cull -Inf/+Inf and NaN.");
                }

                double dist = (worldPointHit - rayParams.Origin).Length;
                Debug.Assert(dist > 0, "Distance is negative: " + dist);

                if (rayParams.HasModelTransformMatrix)
                {
                    rayParams.ModelTransformMatrix.MultiplyPoint(ref pointHit);
                }
                
                rayParams.ReportResult(this, pointHit, dist, i0, i1, i2, barycentric);
            }
        }

        //
        // Processes a ray-line intersection to see if it's a valid hit.
        // 
        // Shares some code with ValidateRayHit
        //
        private void ValidateLineHit(
            RayHitTestParameters rayParams, 
            FaceType facesToHit,
            int i0,
            int i1,
            int i2,
            ref Point3D v0,
            ref Point3D v1,
            ref Point3D v2,
            ref Point barycentric
            )
        {
            Matrix3D worldTransformMatrix = rayParams.HasWorldTransformMatrix ? rayParams.WorldTransformMatrix : Matrix3D.Identity;
                    
            // OK, we have an intersection with the LINE but that could be wrong on three
            // accounts:
            //   1. We could have hit the line on the wrong side of the ray's origin.
            //   2. We may need to cull the intersection if it's beyond the far clipping
            //      plane (only if the hit test originated from a Viewport3DVisual.)
            //   3. We could have hit a back-facing triangle
            // We will transform the hit point back into world space to check these
            // things & compute the correct distance from the origin to the hit point.
            
            // Hit point in model space
            Point3D pointHit = M3DUtil.Interpolate(ref v0, ref v1, ref v2, ref barycentric);
            
            Point3D worldPointHit = pointHit;
            worldTransformMatrix.MultiplyPoint(ref worldPointHit);
            
            // Vector from origin to hit point
            Vector3D hitVector = worldPointHit - rayParams.Origin;
            Vector3D originalDirection = rayParams.Direction;
            double rayDistanceUnnormalized = Vector3D.DotProduct(originalDirection, hitVector);

            if (rayDistanceUnnormalized > 0)
            {
                // If we have a HitTestProjectionMatrix than this hit test originated
                // at a Viewport3DVisual.
                if (rayParams.HasHitTestProjectionMatrix)
                {
                    // To test if we are in front of the far clipping plane what we
                    // do conceptually is project our hit point in world space into
                    // homogenous space and verify that it is on the correct side of
                    // the Z=1 plane.
                    //
                    // To save some cycles we only bother computing Z and W of the
                    // projected point and use a simple Z/W > 1 test to see if we
                    // are past the far plane.
                    //
                    // NOTE: HitTestProjectionMatrix is not just the camera matrices.
                    //       It has an additional translation to move the ray to the
                    //       origin.  This extra translation does not effect this test.
                    
                    Matrix3D m = rayParams.HitTestProjectionMatrix;

                    // We directly substitute 1 for p.W below:
                    double pz = worldPointHit.X * m.M13 + worldPointHit.Y * m.M23 + worldPointHit.Z * m.M33 + m.OffsetZ;
                    double pw = worldPointHit.X * m.M14 + worldPointHit.Y * m.M24 + worldPointHit.Z * m.M34 + m.M44;

                    // Early exit if pz/pw > 1.  The negated logic is to reject NaNs.
                    if (!(pz / pw <= 1))
                    {
                        return;
                    }

                    Debug.Assert(!double.IsInfinity(pz / pw) && !double.IsNaN(pz / pw),
                        "Expected near/far tests to cull -Inf/+Inf and NaN.");
                }

                Point3D a = v0, b = v1, c = v2;

                worldTransformMatrix.MultiplyPoint(ref a);
                worldTransformMatrix.MultiplyPoint(ref b);
                worldTransformMatrix.MultiplyPoint(ref c);

                Vector3D normal = Vector3D.CrossProduct(b - a, c - a);

                double cullSign = -Vector3D.DotProduct(normal, hitVector);
                double det = worldTransformMatrix.Determinant;
                bool frontFace = (cullSign > 0) == (det >= 0);
            
                if (((facesToHit & FaceType.Front) == FaceType.Front && frontFace) || ((facesToHit & FaceType.Back) == FaceType.Back && !frontFace))
                {
                    double dist = hitVector.Length;
                    if (rayParams.HasModelTransformMatrix)
                    {
                        rayParams.ModelTransformMatrix.MultiplyPoint(ref pointHit);
                    }
                    
                    rayParams.ReportResult(this, pointHit, dist, i0, i1, i2, barycentric);
                }
            }          
        }

        
        // Updates the _cachedBounds member to the current bounds of the mesh.
        // This method must be called before accessing _cachedBounds if
        // _cachedBounds.IsEmpty is true.  Otherwise the _cachedBounds are
        // current and do not need to be recomputed.  See also Debug_VerifyCachedBounds.
        private void UpdateCachedBounds()
        {
            Debug.Assert(_cachedBounds.IsEmpty,
                "PERF: Caller should verify that bounds are dirty before recomputing.");
            
            _cachedBounds = M3DUtil.ComputeAxisAlignedBoundingBox(Positions);
        }

        // Sets _cachedBounds to Rect3D.Empty (indicating that the bounds are no
        // longer valid.)
        private void SetCachedBoundsDirty()
        {
            _cachedBounds = Rect3D.Empty;
        }
       
        
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  DEBUG
        //
        //------------------------------------------------------
        
        #region DEBUG

        // Always call this method before accessing _cachedBounds.  On 

        [Conditional("DEBUG")]
        private void Debug_VerifyCachedBounds()
        {
            Rect3D actualBounds = M3DUtil.ComputeAxisAlignedBoundingBox(Positions);

            // The funny boolean logic below avoids asserts when the cached
            // bounds contain NaNs.  (NaN != NaN)
            bool areEqual =
                !(_cachedBounds.X < actualBounds.X || _cachedBounds.X > actualBounds.X) &&
                !(_cachedBounds.Y < actualBounds.Y || _cachedBounds.Y > actualBounds.Y) &&
                !(_cachedBounds.Z < actualBounds.Z || _cachedBounds.Z > actualBounds.Z) &&
                !(_cachedBounds.SizeX < actualBounds.SizeX || _cachedBounds.SizeX > actualBounds.SizeX) &&
                !(_cachedBounds.SizeY < actualBounds.SizeY || _cachedBounds.SizeY > actualBounds.SizeY) &&
                !(_cachedBounds.SizeZ < actualBounds.SizeZ || _cachedBounds.SizeZ > actualBounds.SizeZ);

            if (!areEqual)
            {
                if (_cachedBounds == Rect3D.Empty)
                {
                    Debug.Fail("Cached bounds are invalid. Caller needs to check for IsEmpty and call UpdateCachedBounds.");
                }
                else
                {
                    Debug.Fail("Cached bounds are invalid. We missed a call to SetCachedBoundsDirty.");
                }
            }
        }
        
        #endregion DEBUG
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        // If the _cachedBounds are empty it means that the cache is invalid.  The user must
        // check for this case and call UpdateCachedBounds if the cache is invalid.  (There
        // is no way to distinguish between actually caching "Empty" when there are no
        // positions and the cache being invalid - but computing bounds in this case is
        // very fast.)
        private Rect3D _cachedBounds = Rect3D.Empty;

        #endregion Private Fields
    }
}
