// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

using MS.Internal.Media3D;
using System;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     This is the base class for all 3D geometry classes.  A geometry has
    ///     bounds and can be rendered with a GeometryModel3D.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string        
    public abstract partial class Geometry3D : Animatable, DUCE.IResource
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Geometry3D() {}
        
        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        
        /// <summary>
        ///     Gets bounds for this Geometry3D.
        /// </summary>
        public abstract Rect3D Bounds { get; }
        
        #endregion Public Methods

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

        #region Internal Methods

        // NOTE: Geometry3D hit testing takes the rayParams in the outer space of the
        //       Geometry3D.  That is, RayHitTest() will apply this geometry's 
        //       transform to the ray for the caller.
        //
        //       This is different than Visual hit testing which does not transform
        //       the hit testing parameters by the Visual's transform.
        internal void RayHitTest(RayHitTestParameters rayParams, FaceType facesToHit)
        {
            Debug.Assert(facesToHit != FaceType.None, 
                "Caller should make sure we're trying to hit something");
               
            Rect3D bounds = Bounds;

            if (bounds.IsEmpty)
            {
                return;
            }

            // Geometry3D's do not yet support a Transform property
            //
            // Transform3D transform = Transform;
            // rayParams.PushTransform(transform);


            Point3D origin;
            Vector3D direction;

            rayParams.GetLocalLine(out origin, out direction);

            if (LineUtil.ComputeLineBoxIntersection(ref origin, ref direction, ref bounds, rayParams.IsRay))
            {
                RayHitTestCore(rayParams, facesToHit);
            }
            
            // Geometry3D's do not yet support a Transform property
            //
            // rayParams.PopTransform(transform);
        }

        internal abstract void RayHitTestCore(RayHitTestParameters rayParams, FaceType hitTestableFaces);

        #endregion Internal Methods
    }
}

