// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: 3D matrix transform.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     3D matrix transform.
    /// </summary>
    public sealed partial class MatrixTransform3D : Transform3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public MatrixTransform3D()
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="matrix">Matrix.</param>
        public MatrixTransform3D(Matrix3D matrix)
        {
            Matrix = matrix;
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
        ///     Retrieves matrix representation of transform.
        /// </summary>
        public override Matrix3D Value
        {
            get
            {
                return Matrix;
            }
        }

        /// <summary>
        ///     Whether the transform is affine.
        /// </summary>
        public override bool IsAffine
        {
            get
            {
                return Matrix.IsAffine;
            }
        }

        #endregion Public Properties

        internal override void Append(ref Matrix3D matrix)
        {
            matrix *= Matrix;
        }
    }
}

