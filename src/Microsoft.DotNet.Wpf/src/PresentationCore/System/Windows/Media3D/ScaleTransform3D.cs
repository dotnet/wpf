// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D scale transformation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Internal;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// 3D scale transform.
    /// </summary>
    public sealed partial class ScaleTransform3D : AffineTransform3D
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
        public ScaleTransform3D() {}

        /// <summary>
        ///     Constructor.
        /// </summary>
        public ScaleTransform3D(Vector3D scale)
        {
            ScaleX = scale.X;
            ScaleY = scale.Y;
            ScaleZ = scale.Z;
        }


        /// <summary>
        ///     Constructor.
        /// </summary>
        public ScaleTransform3D(double scaleX, double scaleY, double scaleZ)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        public ScaleTransform3D(Vector3D scale, Point3D center)
        {
            ScaleX = scale.X;
            ScaleY = scale.Y;
            ScaleZ = scale.Z;
            CenterX = center.X;
            CenterY = center.Y;
            CenterZ = center.Z;
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        public ScaleTransform3D(double scaleX, double scaleY, double scaleZ, double centerX, double centerY, double centerZ)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
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
        ///     Retrieves matrix representation of this transform.
        /// </summary>
        public override Matrix3D Value
        {
            get
            {
                ReadPreamble();

                Matrix3D matrix = new Matrix3D();
                Append(ref matrix);

                return matrix;
            }
        }

        #endregion Public Properties

        internal override void Append(ref Matrix3D matrix)
        {
            Vector3D scale = new Vector3D(_cachedScaleXValue, _cachedScaleYValue, _cachedScaleZValue);

            if (_cachedCenterXValue == 0.0 && _cachedCenterYValue == 0.0 && _cachedCenterZValue == 0.0)
            {
                matrix.Scale(scale);
            }
            else
            {
                matrix.ScaleAt(scale, new Point3D(_cachedCenterXValue, _cachedCenterYValue, _cachedCenterZValue));
            }
        }
}
}

