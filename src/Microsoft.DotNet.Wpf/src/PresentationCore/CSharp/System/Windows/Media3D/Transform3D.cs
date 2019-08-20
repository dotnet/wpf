// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D transform implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using MS.Internal.Media3D;
using MS.Internal.PresentationCore;
using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;


namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     3D transformation.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string    
    public abstract partial class Transform3D : GeneralTransform3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Transform3D() {}

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Transforms the given point.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        public new Point3D Transform(Point3D point)
        {
            // this function is included due to forward compatability reasons            
            return base.Transform(point);
        }


        /// <summary>
        ///     Transforms the given vector.
        /// </summary>
        /// <param name="vector">Vector to transform.</param>
        /// <returns>Transformed vector.</returns>
        public Vector3D Transform(Vector3D vector)
        {
            return Value.Transform(vector);
        }

        /// <summary>
        ///     Transforms the given point.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        public Point4D Transform(Point4D point)
        {
            return Value.Transform(point);
        }

        /// <summary>
        ///     Transforms the given list of points.
        /// </summary>
        /// <param name="points">List of points.</param>
        public void Transform(Point3D[] points)
        {
            Value.Transform(points);
        }

        /// <summary>
        ///     Transforms the given list of vectors.
        /// </summary>
        /// <param name="vectors">List of vectors.</param>
        public void Transform(Vector3D[] vectors)
        {
            Value.Transform(vectors);
        }

        /// <summary>
        ///     Transforms the given list of points.
        /// </summary>
        /// <param name="points">List of points.</param>
        public void Transform(Point4D[] points)
        {
            Value.Transform(points);
        }

        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">Input point</param>
        /// <param name="result">Output point</param>
        /// <returns>True if the point was transformed successfuly, false otherwise</returns>
        public override bool TryTransform(Point3D inPoint, out Point3D result)
        {
            result = Value.Transform(inPoint);
            return true;
        }

        /// <summary>
        /// Transforms the bounding box to the smallest axis aligned bounding box
        /// that contains all the points in the original bounding box
        /// </summary>
        /// <param name="rect">Bounding box</param>
        /// <returns>The transformed bounding box</returns>
        public override Rect3D TransformBounds(Rect3D rect)
        {
            return M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref rect, this);
        }

        /// <summary>
        /// Returns the inverse transform if it has an inverse, null otherwise
        /// </summary>        
        public override GeneralTransform3D Inverse
        {
            get
            {
                ReadPreamble();

                Matrix3D matrix = Value;

                if (!matrix.HasInverse)
                {
                    return null;
                }

                matrix.Invert();
                return new MatrixTransform3D(matrix);
            }
        }

        /// <summary>
        /// Returns a best effort affine transform
        /// </summary>
        internal override Transform3D AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                return this;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Identity transformation.
        /// </summary>
        public static Transform3D Identity
        {
            get
            {
                // Make sure identity matrix is initialized.
                if (s_identity == null)
                {
                    MatrixTransform3D identity = new MatrixTransform3D();
                    identity.Freeze();
                    s_identity = identity;
                }
                return s_identity;
            }
        }

        /// <summary>
        ///     Determines whether the matrix is affine.
        /// </summary>
        public abstract bool IsAffine {get;}


        /// <summary>
        ///     Return the current transformation value.
        /// </summary>
        public abstract Matrix3D Value { get; }

        #endregion Public Properties

        internal abstract void Append(ref Matrix3D matrix);
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static Transform3D s_identity;

        #endregion Private Fields
    }
}
