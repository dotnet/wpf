// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Security;
using System.Collections;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;  
using System.Windows.Media.Composition;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    ///<summary>
    /// Create a scale transformation.
    ///</summary>
    public sealed partial class ScaleTransform : Transform
    {
        ///<summary>
        /// Create a scale transformation.
        ///</summary>
        public ScaleTransform()
        {
        }

        ///<summary>
        /// Create a scale transformation.
        ///</summary>
        public ScaleTransform(
            double scaleX,
            double scaleY
            )
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
        }

        ///<summary>
        /// Create a scale transformation.
        ///</summary>
        public ScaleTransform(
            double scaleX,
            double scaleY,
            double centerX,
            double centerY
            ) : this(scaleX, scaleY)
        {
            CenterX = centerX;
            CenterY = centerY;
        }

        ///<summary>
        /// Return the current transformation value.
        ///</summary>
        public override Matrix Value
        {
            get
            {
                ReadPreamble();
                
                Matrix m = new Matrix();

                m.ScaleAt(ScaleX, ScaleY, CenterX, CenterY);

                return m;
            }
        }
        
        ///<summary>
        /// Returns true if transformation matches the identity transform.
        ///</summary>
        internal override bool IsIdentity
        {
            get 
            {
                return ScaleX == 1 && ScaleY == 1 && CanFreeze;
            }
        }
        
        internal override void TransformRect(ref Rect rect)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            double scaleX = ScaleX;
            double scaleY = ScaleY;
            double centerX = CenterX;
            double centerY = CenterY;

            bool translateCenter = centerX != 0 || centerY != 0;
            
            if (translateCenter)
            {
                rect.X -= centerX;
                rect.Y -= centerY;
            }

            rect.Scale(scaleX, scaleY);

            if (translateCenter)
            {
                rect.X += centerX;
                rect.Y += centerY;
            }
        }

        /// <summary>
        /// MultiplyValueByMatrix - *result is set equal to "this" * matrixToMultiplyBy.
        /// </summary>
        /// <param name="result"> The result is stored here. </param>
        /// <param name="matrixToMultiplyBy"> The multiplicand. </param>
        internal override void MultiplyValueByMatrix(ref Matrix result, ref Matrix matrixToMultiplyBy)
        {
            result = Matrix.Identity;

            result._m11 = ScaleX;
            result._m22 = ScaleY;
            double centerX = CenterX;
            double centerY = CenterY;

            result._type = MatrixTypes.TRANSFORM_IS_SCALING;

            if (centerX != 0 || centerY != 0)
            {
                result._offsetX = centerX - centerX * result._m11;
                result._offsetY = centerY - centerY * result._m22;
                result._type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }

            MatrixUtil.MultiplyMatrix(ref result, ref matrixToMultiplyBy);
        }
    }
}
