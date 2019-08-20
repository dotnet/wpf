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
    ///     was a RayHitTestParameter. 
    /// 
    ///     NOTE:  This might have originated as a PointHitTest on a 2D Visual
    ///            which was extended into 3D.
    /// </summary>
    public abstract class RayHitTestResult : HitTestResult
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal RayHitTestResult(Visual3D visualHit, Model3D modelHit) : base (visualHit)
        {
            _modelHit = modelHit;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     Re-expose Visual property strongly typed to Visual3D.
        /// </summary>
        public new Visual3D VisualHit 
        { 
            get { return (Visual3D) base.VisualHit; }
        }

        /// <summary>
        ///     The Model3D intersected by the ray.
        /// </summary>
        public Model3D ModelHit
        {
            get { return _modelHit; }
        }

        /// <summary>
        ///     This is a point in 3-space at which the ray intersected
        ///     the geometry of the hit Model3D.  This point is in the
        ///     local coordinate system of the Model3D.
        /// </summary>
        public abstract Point3D PointHit { get; }

        /// <summary>
        ///     This is the distance between the ray's origin and the
        ///     point hit.
        /// </summary>
        public abstract double DistanceToRayOrigin { get; }

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

        internal abstract void SetDistanceToRayOrigin(double distance);

        internal static int CompareByDistanceToRayOrigin(RayHitTestResult x, RayHitTestResult y)
        {
            return Math.Sign(x.DistanceToRayOrigin - y.DistanceToRayOrigin);
        }
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private readonly Model3D _modelHit;

        #endregion Private Fields
    }
}
