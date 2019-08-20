// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  A rotation in 3-space defined by a Quaternion.
//              
//

using System;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// A rotation in 3-space defined by a Quaternion.
    /// </summary>
    public partial class QuaternionRotation3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor that creates a rotation with Quaternion (0,0,0,1).
        /// </summary>
        public QuaternionRotation3D() {}

        /// <summary>
        /// Constructor taking a quaternion.
        /// </summary>
        public QuaternionRotation3D(Quaternion quaternion)
        {
            Quaternion = quaternion;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        // Used by animation to get a snapshot of the current rotational
        // configuration for interpolation in Rotation3DAnimations.
        internal override Quaternion InternalQuaternion { get { return _cachedQuaternionValue; } }

        #endregion Internal Properties
    }
}
