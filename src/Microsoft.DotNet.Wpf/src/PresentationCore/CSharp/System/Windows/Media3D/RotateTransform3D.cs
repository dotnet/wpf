// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D rotate transforms.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Internal;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// 3D rotate transforms.
    /// </summary>
    public sealed partial class RotateTransform3D : AffineTransform3D
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
        public RotateTransform3D() {}

        /// <summary>
        ///     Constructor from Rotation3D.
        /// </summary>
        /// <param name="rotation">Rotation3D.</param>
        public RotateTransform3D(Rotation3D rotation)
        {
            Rotation = rotation;
        }

        /// <summary>
        ///     Constructor from Rotation3D and center point.
        /// </summary>
        /// <param name="rotation">Rotation3D.</param>
        /// <param name="center">Center point.</param>
        public RotateTransform3D(Rotation3D rotation, Point3D center)
        {
            Rotation = rotation;
            CenterX = center.X;
            CenterY = center.Y;
            CenterZ = center.Z;
        }


        /// <summary>
        ///     Constructor from Rotation3D and center point.
        /// </summary>
        /// <param name="rotation">Rotation3D.</param>
        /// <param name="centerX">X center</param>
        /// <param name="centerY">Y center</param>
        /// <param name="centerZ">Z center</param>
        public RotateTransform3D(Rotation3D rotation, double centerX, double centerY, double centerZ)
        {
            Rotation = rotation;
            CenterX = centerX;
            CenterY = centerY;
            CenterZ = centerZ;
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
        /// Retrieves matrix representing the rotation.
        /// </summary>
        public override Matrix3D Value
        {
            get
            {
                ReadPreamble();

                Rotation3D rotation = _cachedRotationValue;

                if (rotation == null)
                {
                    return Matrix3D.Identity;
                }

                Quaternion quaternion = rotation.InternalQuaternion;
                Point3D center = new Point3D(_cachedCenterXValue, _cachedCenterYValue, _cachedCenterZValue);
                
                return Matrix3D.CreateRotationMatrix(ref quaternion, ref center);
            }
        }

        #endregion Public Properties

        internal override void Append(ref Matrix3D matrix)
        {
            matrix = matrix * Value;
        }
}
}

