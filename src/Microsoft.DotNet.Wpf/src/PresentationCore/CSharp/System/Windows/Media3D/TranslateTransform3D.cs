// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D translate transformation.
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
    ///     3D translate transform.
    /// </summary>
    public sealed partial class TranslateTransform3D : AffineTransform3D
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
        public TranslateTransform3D() {}

        /// <summary>
        ///     Create translation transform.
        /// </summary>
        public TranslateTransform3D(Vector3D offset)
        {
            OffsetX = offset.X;
            OffsetY = offset.Y;
            OffsetZ = offset.Z;
        }

        /// <summary>
        ///     Create translation transform.
        /// </summary>
        public TranslateTransform3D(double offsetX, double offsetY, double offsetZ)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
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
        ///     Returns transform matrix for this transform.
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
            matrix.Translate(new Vector3D(_cachedOffsetXValue, _cachedOffsetYValue, _cachedOffsetZValue));
        }

    }
}

