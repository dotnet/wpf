// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using MS.Internal;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// A rotation in 3-space defined by an axis and an angle to rotate about that axis.
    /// </summary>
    public partial class AxisAngleRotation3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor that creates a rotation with Axis (0,1,0) and Angle of 0.
        /// </summary>
        public AxisAngleRotation3D() {}

        /// <summary>
        /// Constructor taking axis and angle.
        /// </summary>
        public AxisAngleRotation3D(Vector3D axis, double angle)
        {
            Axis = axis;
            Angle = angle;
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

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        // Used by animation to get a snapshot of the current rotational
        // configuration for interpolation in Rotation3DAnimations.
        internal override Quaternion InternalQuaternion
        {
            get
            { 
                if (_cachedQuaternionValue == c_dirtyQuaternion)
                {
                    Vector3D axis = Axis;

                    // Quaternion's axis/angle ctor throws if the axis has zero length.
                    //
                    // This threshold needs to match the one we used in D3DXVec3Normalize (d3dxmath9.cpp)
                    // and in unmanaged code.  See also AxisAngleRotation3D.cpp.
                    if (axis.LengthSquared > DoubleUtil.FLT_MIN)
                    {                    
                        _cachedQuaternionValue = new Quaternion(axis, Angle);
                    }
                    else
                    {
                        // If we have a zero-length axis we return identity (i.e.,
                        // we consider this to be no rotation.)
                        _cachedQuaternionValue = Quaternion.Identity;
                    }               
                }
                
                return _cachedQuaternionValue;
            }
        }

        #endregion Internal Properties

        internal void AxisPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            _cachedQuaternionValue = c_dirtyQuaternion;
        }

        internal void AnglePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            _cachedQuaternionValue = c_dirtyQuaternion;
        }
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Quaternion _cachedQuaternionValue = c_dirtyQuaternion;

        // Arbitrary quaternion that will signify that our cached quat is dirty
        // Reasonable quaternions are normalized so it's very unlikely that this
        // will ever occurr in a normal application.
        internal static readonly Quaternion c_dirtyQuaternion = new Quaternion(
            Math.E, Math.PI, Math.E * Math.PI, 55.0
            );
    }
}
