// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// A rotation in 3-space.
    /// </summary>
    public partial class Rotation3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static Rotation3D()
        {
            // Create our singleton frozen instance
            s_identity = new QuaternionRotation3D();
            s_identity.Freeze();
        }

        // Prevent 3rd parties from extending this abstract base class
        internal Rotation3D() {}

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
        ///     Singleton identity Rotation3D.
        /// </summary>
        public static Rotation3D Identity
        {
            get { return s_identity; }
        }
        
        #endregion Public Properties

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
        internal abstract Quaternion InternalQuaternion
        {
            get;
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        private static readonly Rotation3D s_identity;

        #endregion Private Fields
    }
}
