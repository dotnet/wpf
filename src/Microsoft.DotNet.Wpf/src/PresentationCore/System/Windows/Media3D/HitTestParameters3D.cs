// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MS.Internal.Media3D;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Encapsulates a set parameters for performing a 3D hit test.  This is an
    ///     abstract base class.
    /// </summary>
    /// <remarks>
    ///     Internally the HitTestParameters3D is double as the hit testing "context"
    ///     which includes such things as the current LocalToWorld transform, etc.
    /// </remarks>
    public abstract class HitTestParameters3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Internal to prevent 3rd parties from extending this abstract base class.
        internal HitTestParameters3D() {}

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


        internal void PushVisualTransform(Transform3D transform)
        {
            Debug.Assert(!HasModelTransformMatrix,
                "ModelTransform stack should be empty when pusing a visual transform");
            
            if (transform != null && transform != Transform3D.Identity)
            {            
                _visualTransformStack.Push(transform.Value);
            }
        }

        internal void PushModelTransform(Transform3D transform)
        {
            if (transform != null && transform != Transform3D.Identity)
            {            
                _modelTransformStack.Push(transform.Value);
            }
        }

        internal void PopTransform(Transform3D transform)
        {
            if (transform != null && transform != Transform3D.Identity)
            {
                if (_modelTransformStack.Count > 0)
                {
                    _modelTransformStack.Pop();
                }
                else
                {
                    _visualTransformStack.Pop();
                }
            }
        }
        
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal bool HasWorldTransformMatrix
        {
            get { return _visualTransformStack.Count > 0 || _modelTransformStack.Count > 0; }
        }
        
        internal Matrix3D WorldTransformMatrix
        {
            get
            { 
                Debug.Assert(HasWorldTransformMatrix,
                    "Check HasWorldTransformMatrix before accessing WorldTransformMatrix.");

                if (_modelTransformStack.IsEmpty)
                {
                    return _visualTransformStack.Top;
                }
                else if (_visualTransformStack.IsEmpty)
                {
                    return _modelTransformStack.Top;
                }
                else
                {
                    return _modelTransformStack.Top * _visualTransformStack.Top;
                }
            }
        }


        
        internal bool HasModelTransformMatrix
        {
            get { return _modelTransformStack.Count > 0; }
        }

        /// <summary>
        ///     The ModelTransformMatrix is the transform in the coordinate system
        ///     of the last Visual hit.
        /// </summary>
        internal Matrix3D ModelTransformMatrix
        {
            get
            { 
                Debug.Assert(HasModelTransformMatrix,
                    "Check HasModelTransformMatrix before accessing ModelTransformMatrix.");

                return  _modelTransformStack.Top;
            }
        }
        /// <summary>
        ///     True if the hit test origined in 2D (i.e., we projected a
        ///     the ray from a point on a Viewport3DVisual.)
        /// </summary>
        internal bool HasHitTestProjectionMatrix
        {
            get { return _hitTestProjectionMatrix != null; }
        }
        
        /// <summary>
        ///     The projection matrix can be set to give additional
        ///     information about a hit test that originated from a camera.
        ///     When any 3D point is projected using the matrix it ends up
        ///     at an x,y location (after homogeneous divide) that is a
        ///     constant translation of its location in the camera's
        ///     viewpoint.
        ///
        ///     All points on the ray project to 0,0
        ///
        ///     The RayFromViewportPoint methods will set this matrix up.
        ///     This matrix is needed to implement hit testing of the magic
        ///     line, which is not intersected by any rays not produced
        ///     using RayFromViewportPoint.
        ///
        ///     It is being added to HitTestParameters3D rather than
        ///     RayHitTestParameters3D because it could be generally useful
        ///     to any sort of hit testing that originates from a camera,
        ///     including cone hit testing or the full generalization of 3D
        ///     shape hit testing.
        /// </summary>
        internal Matrix3D HitTestProjectionMatrix
        {
            get
            {
                Debug.Assert(HasHitTestProjectionMatrix,
                    "Check HasHitTestProjectionMatrix before accessing HitTestProjectionMatrix.");
                
                return _hitTestProjectionMatrix.Value;
            }
            
            set
            {
                _hitTestProjectionMatrix = new Matrix3D?(value);
            }
        }

        internal Visual3D CurrentVisual;
        internal Model3D CurrentModel;
        internal GeometryModel3D CurrentGeometry;
        
        #endregion Internal Properties
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Matrix3D? _hitTestProjectionMatrix = null;
        private Matrix3DStack _visualTransformStack = new Matrix3DStack();
        private Matrix3DStack _modelTransformStack = new Matrix3DStack();

        #endregion Private Fields
    }
}

